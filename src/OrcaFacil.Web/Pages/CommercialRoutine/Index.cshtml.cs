using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Web.Extensions;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.CommercialRoutine;

[Authorize]
public sealed class IndexModel(ICommercialAutomationService automation, ICommercialJourneyService journey, ICurrentAccountService account, IPlanAccessService plans, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<RoutineItem> Items { get; private set; } = [];
    public IReadOnlyList<RoutineItem> AllItems { get; private set; } = [];
    public string? LoadError { get; private set; }
    [BindProperty(SupportsGet = true)] public string? Filter { get; set; } = "pending";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (account.AccountId is { } id)
        {
            var decision = await plans.CanUseAsync(id, PlanFeatureCodes.CommercialFollowUpsEnabled, ct);
            if (!decision.IsAllowed)
            {
                TempData.Warning("Os acompanhamentos comerciais estão disponíveis a partir do plano Negócio. Conheça os planos.");
                return RedirectToPage("/Subscription/Index");
            }
        }
        try
        {
            var items = await automation.GetRoutineAsync(false, ct);
            AllItems = items;
            Items = Filter switch
            {
                "expiring" => items.Where(x => x.Kind is "expired" or "expiring").ToArray(),
                "all" => items,
                _ => items.Where(x => x.Priority is "critical" or "important").ToArray()
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to load commercial routine for the active account.");
            LoadError = "Não foi possível carregar a rotina agora. Verifique a saúde do banco ou tente novamente.";
        }
        return Page();
    }
    public async Task<IActionResult> OnPostScheduleAsync(Guid documentId, DateTime? nextFollowUpAt, string? note, CancellationToken ct)
    {
        var result = await journey.ScheduleFollowUpAsync(new(documentId, nextFollowUpAt, note), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostCompleteAsync(Guid documentId, string? note, CancellationToken ct)
    {
        var result = await journey.CompleteFollowUpAsync(documentId, note, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage();
    }
}
