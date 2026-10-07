using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class BudgetAssistantModel : PageModel
{
    private readonly ICurrentAccountService _account;
    private readonly IBudgetAiAssistant _assistant;
    private readonly IAiSuggestionReviewService _reviews;
    private readonly OrcaFacilDbContext _db;
    private readonly IOptions<AiOptions> _options;

    public BudgetAssistantModel(ICurrentAccountService account, IBudgetAiAssistant assistant, IAiSuggestionReviewService reviews, OrcaFacilDbContext db, IOptions<AiOptions> options)
    {
        _account = account;
        _assistant = assistant;
        _reviews = reviews;
        _db = db;
        _options = options;
    }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    public AiBudgetSuggestionReview? Review { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? reviewId, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        if (reviewId.HasValue) Review = await _reviews.FindAsync(accountId, reviewId.Value, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostSuggestAsync(CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        if (!await CanUseAsync(ct)) return Forbid();
        if (string.IsNullOrWhiteSpace(Description) || Description.Trim().Length < 3)
        {
            ErrorMessage = "Descreva o serviço com pelo menos 3 caracteres.";
            return Page();
        }

        var catalog = await _db.ServiceCatalogItems.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Take(200)
            .ToListAsync(ct);
        var permissions = await ServerPermissionsAsync(ct);
        if (!permissions.Contains("documents.create") && !permissions.Contains(PermissionCodes.AiApplySuggestions) && !permissions.Contains(PermissionCodes.AiGenerateDrafts) && !permissions.Contains("Ai.Suggest"))
            return Forbid();
        var settings = await _db.AccountSettings.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        var result = await _assistant.SuggestBudgetAsync(
            new AiRequestContext(accountId, _account.UserId, permissions),
            ServerPolicy(accountId),
            Description.Trim(),
            catalog,
            ct,
            new BudgetCommercialContext(settings?.DefaultQuoteValidityDays, settings?.DefaultDeliveryTerm, settings?.DefaultCommercialTerms, null));
        if (!result.Succeeded)
        {
            ErrorMessage = result.Notice;
            return Page();
        }

        var id = await _reviews.SavePendingAsync(accountId, _account.UserId, result, ct);
        return RedirectToPage(new { reviewId = id });
    }

    public async Task<IActionResult> OnPostApplyAsync(Guid reviewId, Guid[]? selectedCatalogIds, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        if (!await CanUseAsync(ct)) return Forbid();
        var review = await _reviews.FindAsync(accountId, reviewId, ct);
        if (review is null || review.Status != "PendingReview") return NotFound();
        var allowed = review.Items.Select(x => x.CatalogItemId).ToHashSet();
        var selected = (selectedCatalogIds ?? []).Where(allowed.Contains).Distinct().Take(30).ToArray();
        if (selected.Length == 0)
        {
            Review = review;
            ErrorMessage = "Selecione ao menos um item do catálogo antes de criar o rascunho.";
            return Page();
        }

        if (!await _reviews.MarkAsync(accountId, reviewId, "Applied", ct)) return NotFound();
        return Redirect(BuildCreateUrl(selected));
    }

    public async Task<IActionResult> OnPostDismissAsync(Guid reviewId, CancellationToken ct)
    {
        if (_account.AccountId is not Guid accountId) return Forbid();
        if (!await CanUseAsync(ct)) return Forbid();
        if (!await _reviews.MarkAsync(accountId, reviewId, "Dismissed", ct)) return NotFound();
        return RedirectToPage();
    }

    private async Task<bool> CanUseAsync(CancellationToken ct) =>
        await _account.HasPermissionAsync("documents.create", ct)
        || await _account.HasPermissionAsync("Ai.Suggest", ct)
        || await _account.HasPermissionAsync(PermissionCodes.AiApplySuggestions, ct)
        || await _account.HasPermissionAsync(PermissionCodes.AiGenerateDrafts, ct);

    private async Task<HashSet<string>> ServerPermissionsAsync(CancellationToken ct)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in new[] { "documents.create", "Ai.Suggest", PermissionCodes.AiApplySuggestions, PermissionCodes.AiGenerateDrafts })
            if (await _account.HasPermissionAsync(code, ct)) permissions.Add(code);
        return permissions;
    }

    private AiGovernancePolicy ServerPolicy(Guid accountId)
    {
        var denied = _options.Value.Providers.Where(x => !x.Value.Enabled || x.Value.AllowedModels.Count == 0).Select(x => x.Key).ToArray();
        return new AiGovernancePolicy(accountId, AllowSuggestions: true, AllowAutomaticCriticalActions: false, DeniedProviders: denied, AccountActive: true, FeatureEnabled: true);
    }

    private string BuildCreateUrl(IReadOnlyList<Guid> serviceIds)
    {
        var page = Url.Page("/Documents/CreateBudget") ?? "/Documents/CreateBudget";
        return page + "?" + string.Join("&", serviceIds.Select(id => "serviceIds=" + id.ToString("D")));
    }
}
