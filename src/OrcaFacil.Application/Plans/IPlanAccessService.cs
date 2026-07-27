using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Plans;

/// <summary>Single authorization boundary for account plan decisions.</summary>
public interface IPlanAccessService
{
    Task<Subscription?> GetCurrentSubscriptionAsync(Guid accountId, CancellationToken ct = default);
    Task<Plan?> GetSelectedPlanAsync(Guid accountId, CancellationToken ct = default);
    Task<Plan?> GetEffectivePlanAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default);
    Task<PlanVersion?> GetEffectivePlanVersionAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetPlanFeaturesAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default);
    Task<PlanAccessDecision> CanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default);
    Task<int?> GetLimitAsync(Guid accountId, string featureCode, CancellationToken ct = default);
    Task<int> GetUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default);
    Task<int?> GetRemainingUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default);
    Task EnsureCanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default);
    Task InvalidateAccountCacheAsync(Guid accountId, CancellationToken ct = default);
}

public interface IPlanAccessDataSource
{
    Task<Subscription?> GetSubscriptionAsync(Guid accountId, CancellationToken ct);
    Task<PlanOverride?> GetActiveOverrideAsync(Guid accountId, DateTime utcNow, CancellationToken ct);
    Task<PlanVersion?> GetPlanVersionAsync(Guid versionId, CancellationToken ct);
    Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct);
    Task<PlanVersion?> GetPublishedFreeVersionAsync(DateTime utcNow, CancellationToken ct);
    Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetFeaturesAsync(Guid planVersionId, CancellationToken ct);
    Task<int> GetUsageAsync(Guid accountId, string featureCode, DateTime periodStartUtc, CancellationToken ct);
}

public sealed record PlanFeatureSetting(bool? Enabled = null, int? Limit = null, bool IsUnlimited = false);
public sealed record PlanAccessDecision(bool IsAllowed, string FeatureCode, string CurrentPlanCode, string? RequiredPlanCode, int CurrentUsage, int? Limit, string UserMessage, string InternalReason);
