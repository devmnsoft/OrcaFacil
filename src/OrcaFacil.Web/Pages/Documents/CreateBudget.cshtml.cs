using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Security;
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

        var canEdit = await HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) || await HasPermissionAsync("documents.edit", ct);
        if (!canEdit) return StatusCode(403, new { error = "Você não possui permissão para editar documentos." });

        var doc = await _db.Documents.Include(d => d.Items).SingleOrDefaultAsync(d => d.Id == input.DocumentId && d.AccountId == _account.AccountId && !d.IsDeleted, ct);
        if (doc is null) return NotFound(new { error = "Orçamento não encontrado." });

        if (!string.Equals(doc.Status, "Draft", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Apenas orçamentos em rascunho podem receber sugestões de escopo. Revisões já emitidas não podem ser alteradas." });

        Client? client = doc.ClientId.HasValue ? await _db.Clients.SingleOrDefaultAsync(c => c.Id == doc.ClientId.Value && c.AccountId == _account.AccountId && !c.IsDeleted, ct) : null;

        var grantedPermissions = new HashSet<string>(StringComparer.Ordinal);
        if (canEdit) grantedPermissions.Add("documents.edit");
        if (await HasPermissionAsync(PermissionCodes.AiApplySuggestions, ct)) grantedPermissions.Add(PermissionCodes.AiApplySuggestions);

        var context = new OrcaFacil.Application.Ai.AiRequestContext(_account.AccountId.Value, _current.UserId, grantedPermissions);
        var policy = new OrcaFacil.Application.Ai.AiGovernancePolicy(_account.AccountId.Value);
        var correlationId = Guid.NewGuid().ToString("N");

        try
        {
            var review = await _aiReviewer.ReviewQuoteAsync(context, policy, doc, doc.Items, client, ct);
            if (!review.Succeeded)
            {
                return new JsonResult(new
                {
                    succeeded = false,
                    error = "A análise de escopo não pôde ser concluída no momento. Você pode continuar preenchendo manualmente.",
                    correlationId
                });
            }

            var currentText = !string.IsNullOrWhiteSpace(input.Notes) ? input.Notes.Trim() : (doc.Notes ?? "").Trim();
            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrWhiteSpace(currentText))
            {
                sb.AppendLine("Escopo organizado a partir das descrições informadas:");
                var lines = currentText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var line in lines)
                {
                    var clean = line.TrimStart('-', '*', '•', ' ');
                    if (!string.IsNullOrWhiteSpace(clean))
                        sb.AppendLine($"• {clean}");
                }
            }
            else if (doc.Items.Count > 0)
            {
                sb.AppendLine("Escopo proposto com base nos itens cadastrados:");
                foreach (var item in doc.Items.Where(i => !string.IsNullOrWhiteSpace(i.Description)))
                {
                    sb.AppendLine($"• {item.Description.Trim()} (Qtd: {item.Quantity} {item.Unit})");
                }
            }
            else
            {
                sb.AppendLine("Escopo a detalhar conforme alinhamento com o cliente.");
            }

            sb.AppendLine();
            sb.AppendLine("Pontos de atenção e confirmações pendentes [Revisão humana necessária]:");
            sb.AppendLine("• [ ] Prazo estimado de início e conclusão");
            sb.AppendLine("• [ ] Fornecimento de materiais ou insumos (definir se por conta do cliente ou prestador)");
            sb.AppendLine("• [ ] Condições de acesso ao local ou infraestrutura prévia");

            return new JsonResult(new
            {
                succeeded = true,
                improvedNotes = sb.ToString().Trim(),
                suggestedConditions = review.SuggestedConditions ?? doc.ConditionsText,
                findings = review.Findings.Select(f => new { f.Category, f.Severity, f.Message, f.Suggestion }),
                isProviderGenerated = false,
                notice = "Revisão baseada em regras de consistência da proposta. Nenhuma condição ou garantia foi inventada sem o seu consentimento.",
                correlationId
            });
        }
        catch (Exception)
        {
            return new JsonResult(new
            {
                succeeded = false,
                error = "Não foi possível analisar o escopo com o assistente no momento. Você pode continuar preenchendo manualmente.",
                correlationId
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
