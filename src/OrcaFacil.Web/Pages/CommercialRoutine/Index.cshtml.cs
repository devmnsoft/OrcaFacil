using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.CommercialRoutine;

[Authorize]
public sealed class IndexModel(ICommercialAutomationService automation, ICommercialJourneyService journey, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<RoutineItem> Items { get; private set; } = [];
    public IReadOnlyList<RoutineItem> AllItems { get; private set; } = [];
    public string? LoadError { get; private set; }
    [BindProperty(SupportsGet = true)] public string Filter { get; set; } = "pending";

    public async Task OnGetAsync(CancellationToken ct)
    {
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
