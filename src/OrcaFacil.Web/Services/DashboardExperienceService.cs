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
        var dashboardTask = dashboardQueries.GetDashboardAsync(currentUser.UserId, cancellationToken);
        var profileTask = profiles.GetAsync(new(currentUser.UserId), cancellationToken);
        var actionTask = nextBestAction.GetAsync(cancellationToken);
        var planTask = planExperience.GetAsync(cancellationToken);
        var commercialTask = commercialWorkspace.GetDashboardAsync(cancellationToken);
        await Task.WhenAll(dashboardTask, profileTask, actionTask, planTask, commercialTask);

        var dashboard = await dashboardTask;
        var plan = await planTask;
        var firstName = string.IsNullOrWhiteSpace(currentUser.Name)
            ? "bem-vindo"
            : currentUser.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var planUsage = plan.UsageItems
            .Select(item => new DashboardPlanUsage(item.Label, item.Used, item.Limit))
            .ToArray();

        return new(
            firstName,
            dashboard,
            await profileTask is not null,
            await actionTask,
            plan.EffectivePlanName,
            plan.Status,
            plan.DueAt is { } dueAt ? new DateTimeOffset(DateTime.SpecifyKind(dueAt, DateTimeKind.Utc)) : null,
            planUsage,
            plan.ContextualRecommendation,
            await commercialTask);
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
