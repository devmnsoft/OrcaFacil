using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class BudgetAssistantModel : PageModel
{
    private readonly ICurrentAccountService _account;
    private readonly IBudgetAiAssistant _assistant;
    private readonly IAiSuggestionReviewService _reviews;
    private readonly BudgetSuggestionApplyService _apply;
    private readonly IPlanAccessService _plans;
    private readonly OrcaFacilDbContext _db;
    private readonly IOptions<AiOptions> _options;

    public BudgetAssistantModel(ICurrentAccountService account, IBudgetAiAssistant assistant, IAiSuggestionReviewService reviews,
        BudgetSuggestionApplyService apply, IPlanAccessService plans, OrcaFacilDbContext db, IOptions<AiOptions> options)
    {
        _account = account;
        _assistant = assistant;
        _reviews = reviews;
        _apply = apply;
        _plans = plans;
        _db = db;
        _options = options;
    }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public AiBudgetSuggestionReview? Review { get; private set; }
    public IReadOnlyList<AiBudgetSuggestionReview> PendingReviews { get; private set; } = [];
    public bool ExternalAiAllowed { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? reviewId, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        if (!await CanViewAsync(ct)) return Forbid();
        ExternalAiAllowed = await IsExternalAiAllowedAsync(accountId, ct);
        if (reviewId.HasValue) Review = await _reviews.FindAsync(accountId, reviewId.Value, ct);
        PendingReviews = await _reviews.ListPendingAsync(accountId, 5, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostSuggestAsync(CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        if (!await HasAsync(PermissionCodes.AiGenerateDrafts, ct)) return Forbid();
        if (string.IsNullOrWhiteSpace(Description) || Description.Trim().Length < 3)
        {
            ErrorMessage = "Descreva o serviço com pelo menos 3 caracteres.";
            return await PageWithStateAsync(accountId, null, ct);
        }

        var catalog = await _db.ServiceCatalogItems.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Take(200)
            .ToListAsync(ct);
        var permissions = await ServerPermissionsAsync(ct);
        var settings = await _db.AccountSettings.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        var result = await _assistant.SuggestBudgetAsync(
            new AiRequestContext(accountId, _account.UserId, permissions),
            await ResolvePolicyAsync(accountId, ct),
            Description.Trim(),
            catalog,
            ct,
            new BudgetCommercialContext(settings?.DefaultQuoteValidityDays, settings?.DefaultDeliveryTerm, settings?.DefaultCommercialTerms, null));
        if (!result.Succeeded)
        {
            ErrorMessage = result.Notice;
            return await PageWithStateAsync(accountId, null, ct);
        }

        var id = await _reviews.SavePendingAsync(accountId, _account.UserId, result, ct);
        return RedirectToPage(new { reviewId = id });
    }

    public async Task<IActionResult> OnPostApplyAsync(Guid reviewId, Guid[]? selectedCatalogIds, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        // Aplicar exige a permissão específica de aplicação E a de criação de documento:
        // nenhuma das duas, isoladamente, autoriza a operação completa.
        if (!await HasAsync(PermissionCodes.AiApplySuggestions, ct)) return Forbid();
        if (!await HasAsync("documents.create", ct)) return Forbid();

        var selection = BuildSelection(selectedCatalogIds);
        var result = await _apply.ApplyAsync(_account.UserId, accountId, reviewId, selection, ct);
        if (result.Succeeded && result.DocumentId is Guid documentId)
            return RedirectToPage("/Documents/CreateBudget", new { id = documentId });

        ErrorMessage = result.Error ?? "Não foi possível aplicar a sugestão.";
        return await PageWithStateAsync(accountId, reviewId, ct);
    }

    public async Task<IActionResult> OnPostDismissAsync(Guid reviewId, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        if (!await HasAsync(PermissionCodes.AiGenerateDrafts, ct) && !await HasAsync(PermissionCodes.AiApplySuggestions, ct)) return Forbid();
        if (!await _reviews.MarkAsync(accountId, reviewId, "Dismissed", ct)) return NotFound();
        return RedirectToPage();
    }

    private List<BudgetSuggestionApplyItem> BuildSelection(Guid[]? selectedCatalogIds)
    {
        var selected = new List<BudgetSuggestionApplyItem>();
        foreach (var id in (selectedCatalogIds ?? []).Where(x => x != Guid.Empty).Distinct().Take(BudgetSuggestionApplyService.MaxSelectedItems))
        {
            decimal? quantity = null;
            var raw = Request.Form[$"qty_{id:N}"].ToString();
            if (!string.IsNullOrWhiteSpace(raw)
                && decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                quantity = parsed;
            selected.Add(new(id, quantity));
        }
        return selected;
    }

    private async Task<IActionResult> PageWithStateAsync(Guid accountId, Guid? reviewId, CancellationToken ct)
    {
        ExternalAiAllowed = await IsExternalAiAllowedAsync(accountId, ct);
        if (reviewId.HasValue) Review = await _reviews.FindAsync(accountId, reviewId.Value, ct);
        PendingReviews = await _reviews.ListPendingAsync(accountId, 5, ct);
        return Page();
    }

    private async Task<bool> CanViewAsync(CancellationToken ct) =>
        await HasAsync(PermissionCodes.AiView, ct)
        || await HasAsync(PermissionCodes.AiGenerateDrafts, ct)
        || await HasAsync(PermissionCodes.AiApplySuggestions, ct);

    private async Task<bool> HasAsync(string permissionCode, CancellationToken ct)
    {
        try { return await _account.HasPermissionAsync(permissionCode, ct); }
        catch (UnauthorizedAccessException) { return false; }
    }

    private async Task<HashSet<string>> ServerPermissionsAsync(CancellationToken ct)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in new[] { PermissionCodes.AiGenerateDrafts, PermissionCodes.AiApplySuggestions, "documents.create" })
            if (await HasAsync(code, ct)) permissions.Add(code);
        return permissions;
    }

    private async Task<bool> IsExternalAiAllowedAsync(Guid accountId, CancellationToken ct) =>
        (await _plans.CanUseAsync(accountId, PlanFeatureCodes.AiExternalProvidersEnabled, ct)).IsAllowed;

    /// <summary>
    /// Política real resolvida no servidor: vínculo e conta ativa são comprovados no banco,
    /// o benefício de IA externa vem do plano efetivo e os provedores negados vêm da configuração.
    /// Nenhum destes valores é aceito do cliente.
    /// </summary>
    private async Task<AiGovernancePolicy> ResolvePolicyAsync(Guid accountId, CancellationToken ct)
    {
        await _account.EnsureAccountAccessAsync(ct);
        var denied = _options.Value.Providers.Where(x => !x.Value.Enabled || x.Value.AllowedModels.Count == 0).Select(x => x.Key).ToArray();
        var externalAllowed = await IsExternalAiAllowedAsync(accountId, ct);
        return new AiGovernancePolicy(accountId,
            AllowSuggestions: true,
            AllowAutomaticCriticalActions: false,
            DeniedProviders: denied,
            AccountActive: _account.AccountStatus is null or AccountStatus.Active,
            FeatureEnabled: externalAllowed);
    }
}
