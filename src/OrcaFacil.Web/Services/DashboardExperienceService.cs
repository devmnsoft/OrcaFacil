using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Application.Profile;
using OrcaFacil.Application.Commercial;

namespace OrcaFacil.Web.Services;

public interface IDashboardExperienceService
{
    Task<DashboardExperienceViewModel> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Composes the dashboard from account data so presentation code never invents plan or renewal information.</summary>
public sealed class DashboardExperienceService(
    ICurrentUserService currentUser,
    IDashboardQueries dashboardQueries,
    ProfileService profiles,
    INextBestActionService nextBestAction,
    IPlanExperienceService planExperience,
    ICommercialWorkspaceQueryService commercialWorkspace) : IDashboardExperienceService
{
    public async Task<DashboardExperienceViewModel> GetAsync(CancellationToken cancellationToken = default)
    {
        // These collaborators are scoped and ultimately share OrcaFacilDbContext.
        // Await each operation before starting the next one: EF contexts do not support
        // multiple active operations, even when every individual query is asynchronous.
        var dashboard = await dashboardQueries.GetDashboardAsync(currentUser.UserId, cancellationToken);
        var profile = await profiles.GetAsync(new(currentUser.UserId), cancellationToken);
        var action = await nextBestAction.GetAsync(cancellationToken);
        var plan = await planExperience.GetAsync(cancellationToken);
        var commercial = await commercialWorkspace.GetDashboardAsync(cancellationToken);
        var firstName = string.IsNullOrWhiteSpace(currentUser.Name)
            ? "bem-vindo"
            : currentUser.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var planUsage = plan.UsageItems
            .Select(item => new DashboardPlanUsage(item.Label, item.Used, item.Limit))
            .ToArray();

        return new(
            firstName,
            dashboard,
            profile is not null,
            action,
            plan.EffectivePlanName,
            plan.Status,
            plan.DueAt is { } dueAt ? new DateTimeOffset(DateTime.SpecifyKind(dueAt, DateTimeKind.Utc)) : null,
            planUsage,
            plan.ContextualRecommendation,
            commercial);
    }
}

public sealed record DashboardExperienceViewModel(
    string FirstName,
    DashboardDto Metrics,
    bool HasIssuerProfile,
    NextBestAction NextAction,
    string PlanName,
    string PlanStatus,
    DateTimeOffset? PlanDueAt,
    IReadOnlyList<DashboardPlanUsage> PlanUsage,
    string PlanRecommendation,
    CommercialDashboardView Commercial);

public sealed record DashboardPlanUsage(string Label, int Used, int? Limit);
