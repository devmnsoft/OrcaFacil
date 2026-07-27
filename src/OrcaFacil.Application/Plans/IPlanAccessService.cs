using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Plans;

public interface IPlanAccessService
{
    Task<string> GetEffectivePlanAsync(Subscription? subscription, DateTime utcNow, PlanOverride? activeOverride = null, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetPlanFeaturesAsync(string planCode, CancellationToken ct = default);
    Task<PlanAccessDecision> CanUseAsync(string planCode, string featureCode, int currentUsage = 0, CancellationToken ct = default);
    Task<int?> GetLimitAsync(string planCode, string featureCode, CancellationToken ct = default);
    Task<int> GetUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default);
    Task EnsureCanUseAsync(string planCode, string featureCode, int currentUsage = 0, CancellationToken ct = default);
    Task<string?> GetUpgradeSuggestionAsync(string planCode, string featureCode, CancellationToken ct = default);
}
public sealed record PlanFeatureSetting(bool? Enabled = null, int? Limit = null, bool IsUnlimited = false);
public sealed record PlanAccessDecision(bool IsAllowed, string FeatureCode, string CurrentPlanCode, string? RequiredPlanCode, int CurrentUsage, int? Limit, string UserMessage, string InternalReason);
