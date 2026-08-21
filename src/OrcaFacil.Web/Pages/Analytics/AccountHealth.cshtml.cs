using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc.RazorPages; using OrcaFacil.Application.Security; using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Analytics;
[Authorize(Policy = "Permission:" + PermissionCodes.AccountHealthView)] public sealed class AccountHealthModel(AnalyticsV21Service analytics):PageModel { public AccountHealthView Health {get;private set;}=default!; public async Task OnGetAsync(CancellationToken ct)=>Health=await analytics.AccountHealthAsync(ct); }
