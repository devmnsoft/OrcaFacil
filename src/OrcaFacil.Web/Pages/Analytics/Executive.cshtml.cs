using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Security;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Analytics;

[Authorize(Policy = "Permission:" + PermissionCodes.AnalyticsExecutive)]
public sealed class ExecutiveModel(AnalyticsV21Service analytics) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? To { get; set; }
    public AnalyticsDashboard Dashboard { get; private set; } = default!;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow); var start = From ?? new DateOnly(today.Year, today.Month, 1); var end = To ?? today;
        if (end < start) ModelState.AddModelError(string.Empty, "A data final deve ser posterior à inicial.");
        if (!ModelState.IsValid) { start = new DateOnly(today.Year, today.Month, 1); end = today; }
        Dashboard = await analytics.DashboardAsync(start, end, ct); From = start; To = end; return Page();
    }
}
