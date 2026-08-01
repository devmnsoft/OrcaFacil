using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDashboardExperienceService _experience;
    public DashboardExperienceViewModel Experience { get; private set; } = null!;

    public IndexModel(IDashboardExperienceService experience) => _experience = experience;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Experience = await _experience.GetAsync(ct);
    }
}
