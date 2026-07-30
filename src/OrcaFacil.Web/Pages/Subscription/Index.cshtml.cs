using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Subscription;
[Authorize]
public sealed class IndexModel(IPlanExperienceService experience) : PageModel
{
 public PlanExperienceViewModel Plan { get; private set; } = default!;
 public async Task OnGetAsync(CancellationToken ct) => Plan=await experience.GetAsync(ct);
}
