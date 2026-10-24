using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Clients;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.Plans;
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
    private readonly IClientWorkspaceService _clients;
    private readonly IPlanAccessService _plans;
    public CreateBudgetModel(ICurrentUserService current, ICurrentAccountService account, BudgetWizardService wizard, OrcaFacilDbContext db, ICommercialAiReviewer aiReviewer, IClientWorkspaceService clients, IPlanAccessService plans)
    { _current = current; _account = account; _wizard = wizard; _db = db; _aiReviewer = aiReviewer; _clients = clients; _plans = plans; }

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

    public async Task<IActionResult> OnGetBudgetCatalogAsync(string? q, int? limit, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var canEdit = await HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) || await HasPermissionAsync("documents.edit", ct);
        if (!canEdit) return StatusCode(403, new { error = "Você não possui permissão para editar documentos." });

        var query = _db.ServiceCatalogItems.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.IsActive && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = $"%{q.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, term)
                || (x.Code != null && EF.Functions.ILike(x.Code, term))
                || (x.Description != null && EF.Functions.ILike(x.Description, term))
                || (x.Tags != null && EF.Functions.ILike(x.Tags, term)));
        }

        var take = Math.Clamp(limit ?? 8, 1, 20);
        var items = await query
            .OrderByDescending(x => x.IsFavorite)
            .ThenByDescending(x => x.LastUsedAt)
            .ThenByDescending(x => x.UseCount)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToListAsync(ct);
        var results = items.Select(ToBudgetCatalogChoice).ToList();

        return new JsonResult(new { results });
    }

    public async Task<IActionResult> OnGetClientSearchAsync(string? q, int? page, int? pageSize, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var canViewClients = await HasPermissionAsync(PermissionCodes.ClientsView, ct)
            || await HasPermissionAsync(PermissionCodes.ClientsManage, ct)
            || await HasPermissionAsync(PermissionCodes.DocumentsCreate, ct)
            || await HasPermissionAsync(PermissionCodes.DocumentsEdit, ct);
        if (!canViewClients) return StatusCode(403, new { error = "Você não possui permissão para consultar clientes." });

        var take = Math.Clamp(pageSize ?? 12, 1, 25);
        var skip = (Math.Max(page ?? 1, 1) - 1) * take;
        var query = _db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            var term = $"%{search}%";
            var digits = new string(search.Where(char.IsDigit).ToArray());
            query = query.Where(x => EF.Functions.ILike(x.Name, term)
                || (x.TradeName != null && EF.Functions.ILike(x.TradeName, term))
                || (x.DocumentNumber != null && x.DocumentNumber.Contains(digits.Length == 0 ? search : digits))
                || (x.Phone != null && x.Phone.Contains(digits.Length == 0 ? search : digits))
                || (x.Email != null && EF.Functions.ILike(x.Email, term))
                || _db.ClientContacts.Any(c => c.AccountId == accountId && c.ClientId == x.Id && !c.IsDeleted &&
                    (EF.Functions.ILike(c.Name, term) || EF.Functions.ILike(c.Value, term))));
        }

        var total = await query.CountAsync(ct);
        var results = await query.OrderBy(x => x.Name).Skip(skip).Take(take)
            .Select(x => new ClientChoice(x.Id, x.Name, x.DocumentNumber, x.Phone, x.Email, x.City))
            .ToListAsync(ct);
        return new JsonResult(new { results, total, page = Math.Max(page ?? 1, 1), pageSize = take });
    }

    public sealed record QuickClientRequest(string Name, string? Phone, string? Email, string? City, string? DocumentNumber, bool AllowPossibleDuplicate);

    public async Task<IActionResult> OnPostQuickClientAsync([FromBody] QuickClientRequest input, CancellationToken ct)
    {
        if (_account.AccountId is null) return Forbid();
        try { await _account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var canCreateClient = await HasPermissionAsync(PermissionCodes.ClientsCreate, ct)
            || await HasPermissionAsync(PermissionCodes.ClientsManage, ct);
        if (!canCreateClient) return StatusCode(403, new { error = "Você não possui permissão para cadastrar clientes." });

        if (string.IsNullOrWhiteSpace(input.Name))
            return BadRequest(new { error = "Informe o nome do cliente." });

        var client = new Client
        {
            PersonType = InferPersonType(input.DocumentNumber),
            DocumentType = InferDocumentType(input.DocumentNumber),
            DocumentNumber = input.DocumentNumber,
            Name = input.Name,
            Phone = input.Phone,
            Email = input.Email,
            City = input.City,
            IsActive = true
        };

        var result = await _clients.CreateAsync(client, input.AllowPossibleDuplicate, ct);
        if (result.Code == ClientResultCode.Success && result.ClientId.HasValue)
            return new JsonResult(new { client = ToClientChoice(client) });

        if ((result.Code == ClientResultCode.PossibleDuplicate || result.Code == ClientResultCode.DuplicateDocument) && result.Candidates is { Count: > 0 })
        {
            var candidates = result.Candidates.Select(x => new { id = x.Id, name = x.Name, reason = x.MatchReason }).ToList();
            return StatusCode(result.Code == ClientResultCode.DuplicateDocument ? 409 : 422,
                new { error = result.Message ?? "Encontramos clientes parecidos.", code = result.Code.ToString(), candidates });
        }

        return BadRequest(new { error = result.Message ?? "Não foi possível cadastrar o cliente." });
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

        // Governança igual aos demais recursos de IA do orçamento: conta ativa e IA externa
        // liberada pelo plano (AiExternalProvidersEnabled, padrão do BudgetAssistant).
        var accountActive = _account.AccountStatus is null or AccountStatus.Active;
        var externalAiAllowed = accountActive
            && (await _plans.CanUseAsync(_account.AccountId.Value, PlanFeatureCodes.AiExternalProvidersEnabled, ct)).IsAllowed;
        var context = new OrcaFacil.Application.Ai.AiRequestContext(_account.AccountId.Value, _current.UserId, grantedPermissions);
        var policy = new OrcaFacil.Application.Ai.AiGovernancePolicy(_account.AccountId.Value, AccountActive: accountActive, FeatureEnabled: externalAiAllowed);
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
                isProviderGenerated = !review.IsRuleBased,
                notice = review.IsRuleBased
                    ? "Revisão baseada em regras de consistência da proposta. Nenhuma condição ou garantia foi inventada sem o seu consentimento."
                    : "Revisão sugerida com apoio de IA a partir apenas dos dados desta proposta. Confira os apontamentos antes de aplicar — nada foi alterado no orçamento.",
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

    public static BudgetCatalogChoice ToBudgetCatalogChoice(ServiceCatalogItem item) => new(item.Id, item.Name,
        item.Description, item.UnitCode, item.StandardPrice, item.SuggestedDurationMinutes, item.DefaultDeliveryTerm,
        item.DefaultNotes, item.IsFavorite, item.IsRecurring);

    private static ClientChoice ToClientChoice(Client client) => new(client.Id, client.Name, client.DocumentNumber, client.Phone, client.Email, client.City);

    private static BrazilianDocumentType? InferDocumentType(string? documentNumber)
    {
        var digits = new string((documentNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length switch { 11 => BrazilianDocumentType.CPF, 14 => BrazilianDocumentType.CNPJ, _ => null };
    }

    private static PersonType InferPersonType(string? documentNumber) => InferDocumentType(documentNumber) == BrazilianDocumentType.CNPJ
        ? PersonType.Company
        : PersonType.Individual;
}

public sealed record ClientChoice(Guid Id, string Name, string? Document, string? Phone, string? Email, string? City);
public sealed record BudgetCatalogChoice(Guid Id, string Name, string? Description, string Unit, decimal Price,
    int? DurationMinutes, string? DeliveryTerm, string? DefaultNotes, bool Favorite, bool Recurring);
