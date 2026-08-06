using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDashboardExperienceService _experience;
    private readonly OrcaFacil.Application.Onboarding.IOnboardingApplicationService _onboarding;
    public OrcaFacil.Application.Onboarding.OnboardingStateView? Onboarding { get; private set; }
    public DashboardExperienceViewModel Experience { get; private set; } = null!;

    public IndexModel(IDashboardExperienceService experience, OrcaFacil.Application.Onboarding.IOnboardingApplicationService onboarding) { _experience = experience; _onboarding = onboarding; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Experience = await _experience.GetAsync(ct);
        Onboarding = (await _onboarding.GetAsync(ct)).Value;
    }
}
