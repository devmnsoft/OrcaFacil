using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDashboardExperienceService _experience;
    private readonly OrcaFacil.Application.Onboarding.IOnboardingApplicationService _onboarding;
    private readonly IIntelligenceReportService _reports;
    private readonly IOperationalAlertService _alerts;
    public OrcaFacil.Application.Onboarding.OnboardingStateView? Onboarding { get; private set; }
    public DashboardExperienceViewModel Experience { get; private set; } = null!;
    public IntelligenceReport Financial { get; private set; } = new("Financeiro", [], []);
    public OrcaFacil.Domain.Entities.RecommendationCard? BestRecommendation { get; private set; }
    public IReadOnlySet<string> AvailableModules { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private readonly IRecommendationService _recommendations;
    private readonly IUserMenuComposer _menu;
    public IndexModel(IDashboardExperienceService experience, OrcaFacil.Application.Onboarding.IOnboardingApplicationService onboarding, IIntelligenceReportService reports, IOperationalAlertService alerts, IRecommendationService recommendations, IUserMenuComposer menu) { _experience = experience; _onboarding = onboarding; _reports = reports; _alerts = alerts; _recommendations = recommendations; _menu = menu; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        AvailableModules = (await _menu.ComposeAsync(ct)).Select(x => x.ModuleCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Experience = await _experience.GetAsync(ct);
        Onboarding = (await _onboarding.GetAsync(ct)).Value;
        if (AvailableModules.Contains("FINANCIAL"))
            Financial = await _reports.FinancialAsync(new(DateTime.UtcNow.Date.AddDays(1 - DateTime.UtcNow.Day), DateTime.UtcNow.Date), ct);
        BestRecommendation = (await _recommendations.GetOpenAsync(ct)).FirstOrDefault();
        await _alerts.GenerateAsync(ct);
    }
}
