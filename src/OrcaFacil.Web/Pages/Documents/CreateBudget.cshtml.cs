using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class CreateBudgetModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly ICurrentAccountService _account;
    private readonly BudgetWizardService _wizard;
    private readonly OrcaFacilDbContext _db;
    private readonly ICommercialAiReviewer _aiReviewer;
    public CreateBudgetModel(ICurrentUserService current, ICurrentAccountService account, BudgetWizardService wizard, OrcaFacilDbContext db, ICommercialAiReviewer aiReviewer)
    { _current = current; _account = account; _wizard = wizard; _db = db; _aiReviewer = aiReviewer; }

    public BudgetWizardViewModel Draft { get; private set; } = default!;
    public IReadOnlyList<ClientChoice> Clients { get; private set; } = [];

    public string? OpenError { get; private set; }

    /// <summary>
    /// GET apenas consulta: abre um rascunho existente. Nenhum documento é criado
    /// como efeito de GET, atualização de página ou redirecionamento — a criação
    /// ocorre somente pelo comando POST Create, com proteção antiforgery.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        if (!id.HasValue) return RedirectToPage("/Documents/New");
        if (!await HasPermissionAsync("documents.edit", ct)) return Forbid();
        var opened = await _wizard.OpenAsync(_current.UserId, _account.AccountId, id.Value, null, ct);
        if (!opened.Succeeded || opened.Draft is null)
        {
            OpenError = opened.Error ?? "Não foi possível abrir o orçamento.";
            return Page();
        }
        Draft = opened.Draft;
        await LoadClients(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(Guid? clientId, Guid[]? serviceIds, Guid? templateId, string? idempotencyKey, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        if (!await HasPermissionAsync("documents.create", ct)) return Forbid();
        var opened = await _wizard.OpenAsync(_current.UserId, _account.AccountId, null, clientId, ct,
            serviceIds is { Length: > 0 } ? serviceIds : [], templateId, idempotencyKey);
        if (!opened.Succeeded || opened.Draft is null)
        {
            OpenError = opened.Error ?? "Não foi possível criar o orçamento.";
            return Page();
        }
        return RedirectToPage(new { id = opened.Draft.DocumentId });
    }

    public async Task<IActionResult> OnPostAutosaveAsync([FromBody] SaveBudgetDraftRequest input, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        if (!await HasPermissionAsync("documents.edit", ct)) return StatusCode(403, new { error = "Você não possui permissão para editar documentos." });
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey)) return BadRequest(new { error = "Identificador de salvamento ausente." });
        var result = await _wizard.SaveAsync(_current.UserId, _account.AccountId, input, ct);
        return result.Succeeded
            ? new JsonResult(new { draft = result.Draft, notices = result.Notices ?? [], rowVersion = result.Draft?.RowVersion })
            : StatusCode(result.Conflict ? 409 : 400, new { error = result.Error, draft = result.Draft, conflict = result.Conflict });
    }

    public async Task<IActionResult> OnPostFinalizeAsync([FromBody] SaveBudgetDraftRequest input, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        if (!await HasPermissionAsync("documents.edit", ct)) return StatusCode(403, new { error = "Você não possui permissão para finalizar documentos." });
        var result = await _wizard.FinalizeAsync(_current.UserId, _account.AccountId, input, ct);
        return result.Succeeded ? new JsonResult(new { redirectUrl = Url.Page("/Documents/Details", new { id = input.DocumentId }) })
            : StatusCode(result.Conflict ? 409 : 400, new { error = result.Error, draft = result.Draft });
    }

    public sealed record AssistScopeRequest(Guid DocumentId, string? Description, string? Notes, string? Conditions);

    public async Task<IActionResult> OnPostAssistScopeAsync([FromBody] AssistScopeRequest input, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var doc = await _db.Documents.Include(d => d.Items).SingleOrDefaultAsync(d => d.Id == input.DocumentId && d.AccountId == _account.AccountId && !d.IsDeleted, ct);
        if (doc is null) return NotFound(new { error = "Orçamento não encontrado." });

        Client? client = doc.ClientId.HasValue ? await _db.Clients.SingleOrDefaultAsync(c => c.Id == doc.ClientId.Value && c.AccountId == _account.AccountId && !c.IsDeleted, ct) : null;

        var context = new OrcaFacil.Application.Ai.AiRequestContext(_account.AccountId.Value, _current.UserId, new HashSet<string> { "documents.edit" });
        var policy = new OrcaFacil.Application.Ai.AiGovernancePolicy(_account.AccountId.Value);

        try
        {
            var review = await _aiReviewer.ReviewQuoteAsync(context, policy, doc, doc.Items, client, ct);
            var currentText = !string.IsNullOrWhiteSpace(input.Notes) ? input.Notes : doc.Notes ?? "";
            var improved = string.IsNullOrWhiteSpace(currentText)
                ? "Serviço com escopo organizado:\n• Execução conforme especificações técnicas alinhadas\n• Materiais e mão de obra inclusos\n• Garantia e limpeza do local após a entrega"
                : $"Escopo detalhado:\n{currentText.Trim()}\n\n• Atendimento às normas técnicas aplicáveis\n• Entrega no prazo combinado com aceite formal";

            return new JsonResult(new
            {
                succeeded = true,
                improvedNotes = improved,
                suggestedConditions = review.SuggestedConditions ?? doc.ConditionsText,
                findings = review.Findings.Select(f => new { f.Category, f.Severity, f.Message, f.Suggestion })
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                succeeded = false,
                error = "Não foi possível gerar a sugestão com IA no momento. Você pode continuar preenchendo manualmente.",
                detail = ex.Message
            });
        }
    }

    private async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken ct)
    {
        try { return await _account.HasPermissionAsync(permissionCode, ct); }
        catch (UnauthorizedAccessException) { return false; }
    }

    private async Task LoadClients(CancellationToken ct) => Clients = await _db.Clients.AsNoTracking()
        .Where(x => x.AccountId == _account.AccountId && !x.IsDeleted).OrderBy(x => x.Name).Take(40)
        .Select(x => new ClientChoice(x.Id, x.Name, x.DocumentNumber, x.Phone, x.Email, x.City)).ToListAsync(ct);
}

public sealed record ClientChoice(Guid Id, string Name, string? Document, string? Phone, string? Email, string? City);
