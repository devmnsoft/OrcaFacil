using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.CommercialRoutine;
[Authorize] public sealed class PendingQuotesModel(ICommercialAutomationService automation) : PageModel
{
 public IReadOnlyList<RoutineItem> Items { get; private set; }=[];
 public async Task OnGetAsync(CancellationToken ct)=>Items=await automation.GetRoutineAsync(true,ct);
}
