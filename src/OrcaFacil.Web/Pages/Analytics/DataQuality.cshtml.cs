using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc.RazorPages; using OrcaFacil.Application.Security; using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Analytics;
[Authorize(Policy = "Permission:" + PermissionCodes.DataQualityView)] public sealed class DataQualityModel(AnalyticsV21Service analytics) : PageModel { public IReadOnlyList<QualityFindingView> Findings {get;private set;}=[]; public async Task OnGetAsync(CancellationToken ct)=>Findings=await analytics.DataQualityAsync(ct); }
