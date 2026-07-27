using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Plans;

public sealed class PlanAccessService : IPlanAccessService
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, PlanFeatureSetting>> Features = PlanCatalogDefinitions.Features;

    public Task<string> GetEffectivePlanAsync(Subscription? subscription, DateTime utcNow, PlanOverride? activeOverride = null, CancellationToken ct = default)
    {
        if (activeOverride?.IsEffective(utcNow) == true) return Task.FromResult("OVERRIDE");
        if (subscription is null || subscription.Status is SubscriptionStatus.Free or SubscriptionStatus.Cancelled) return Task.FromResult("FREE");
        if (subscription.ManualReleaseUntil > utcNow) return Task.FromResult(Normalize(subscription.Plan));
        var due = subscription.PaidThroughAt ?? subscription.ExpiresAt ?? subscription.NextDueAt;
        return Task.FromResult(due.HasValue && due.Value < utcNow ? "FREE" : Normalize(subscription.Plan));
    }
    public Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetPlanFeaturesAsync(string planCode, CancellationToken ct = default) => Task.FromResult(Features.TryGetValue(planCode.ToUpperInvariant(), out var value) ? value : Features["FREE"]);
    public Task<PlanAccessDecision> CanUseAsync(string planCode, string featureCode, int currentUsage = 0, CancellationToken ct = default)
    {
        var code = Features.ContainsKey(planCode.ToUpperInvariant()) ? planCode.ToUpperInvariant() : "FREE";
        Features[code].TryGetValue(featureCode, out var setting);
        var allowed = setting is not null && setting.Enabled != false && (setting.IsUnlimited || setting.Limit is null || currentUsage < setting.Limit);
        var required = allowed ? null : Features.FirstOrDefault(x => x.Value.TryGetValue(featureCode, out var candidate) && candidate.Enabled != false && (candidate.IsUnlimited || candidate.Limit is null || currentUsage < candidate.Limit)).Key;
        return Task.FromResult(new PlanAccessDecision(allowed, featureCode, code, required, currentUsage, setting?.Limit, allowed ? string.Empty : "Este recurso está pausado. Assim que estiver disponível no seu plano, ele será liberado.", allowed ? "Allowed" : setting is null ? "FeatureNotConfigured" : "PlanLimitReached"));
    }
    public async Task<int?> GetLimitAsync(string planCode, string featureCode, CancellationToken ct = default) => (await GetPlanFeaturesAsync(planCode, ct)).TryGetValue(featureCode, out var value) && !value.IsUnlimited ? value.Limit : null;
    public Task<int> GetUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult(0);
    public async Task EnsureCanUseAsync(string planCode, string featureCode, int currentUsage = 0, CancellationToken ct = default) { var decision = await CanUseAsync(planCode, featureCode, currentUsage, ct); if (!decision.IsAllowed) throw new PlanAccessDeniedException(decision); }
    public async Task<string?> GetUpgradeSuggestionAsync(string planCode, string featureCode, CancellationToken ct = default) => (await CanUseAsync(planCode, featureCode, ct: ct)).RequiredPlanCode;
    private static string Normalize(PlanType plan) => plan switch { PlanType.Business => "BUSINESS", PlanType.Pro => "PROFESSIONAL", _ => "FREE" };
}
public sealed class PlanAccessDeniedException(PlanAccessDecision decision) : InvalidOperationException(decision.UserMessage) { public PlanAccessDecision Decision { get; } = decision; }
