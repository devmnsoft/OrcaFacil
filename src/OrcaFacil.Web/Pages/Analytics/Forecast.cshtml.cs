using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Security;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Analytics;
[Authorize(Policy = "Permission:" + PermissionCodes.AnalyticsForecast)]
public sealed class ForecastModel(AnalyticsV21Service analytics) : PageModel
{
 public AnalyticsDashboard Dashboard { get; private set; } = default!;
 public async Task OnGetAsync(CancellationToken ct) { var today=DateOnly.FromDateTime(DateTime.UtcNow); Dashboard=await analytics.DashboardAsync(today, today.AddDays(30), ct); }
}
