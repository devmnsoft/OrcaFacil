using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.CommercialRoutine;

[Authorize]
public sealed class IndexModel(ICommercialAutomationService automation, ICommercialJourneyService journey) : PageModel
{
    public IReadOnlyList<RoutineItem> Items { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) => Items = await automation.GetRoutineAsync(false, ct);
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
