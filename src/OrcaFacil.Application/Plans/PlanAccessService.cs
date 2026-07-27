using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Plans;

public sealed class PlanAccessService(IPlanAccessDataSource dataSource) : IPlanAccessService
{
    public Task<Subscription?> GetCurrentSubscriptionAsync(Guid accountId, CancellationToken ct = default) =>
        dataSource.GetSubscriptionAsync(accountId, ct);

    public async Task<Plan?> GetSelectedPlanAsync(Guid accountId, CancellationToken ct = default)
    {
        var subscription = await dataSource.GetSubscriptionAsync(accountId, ct);
        if (subscription?.SelectedPlanVersionId is not Guid versionId) return null;
        var version = await dataSource.GetPlanVersionAsync(versionId, ct);
        return version is null ? null : await dataSource.GetPlanAsync(version.PlanId, ct);
    }

    public async Task<PlanVersion?> GetEffectivePlanVersionAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default)
    {
        utcNow = EnsureUtc(utcNow);
        var activeOverride = await dataSource.GetActiveOverrideAsync(accountId, utcNow, ct);
        if (activeOverride is not null)
            return await dataSource.GetPlanVersionAsync(activeOverride.PlanVersionId, ct);

        var subscription = await dataSource.GetSubscriptionAsync(accountId, ct);
        if (subscription is null) return await dataSource.GetPublishedFreeVersionAsync(utcNow, ct);

        var dueAt = subscription.PaidThroughAt ?? subscription.ExpiresAt ?? subscription.NextDueAt;
        var paidAccess = subscription.Status is not (SubscriptionStatus.Free or SubscriptionStatus.Cancelled or SubscriptionStatus.Suspended)
                         && (subscription.ManualReleaseUntil > utcNow || dueAt is null || dueAt >= utcNow);
        var versionId = paidAccess ? subscription.EffectivePlanVersionId ?? subscription.SelectedPlanVersionId : null;
        return versionId is Guid id
            ? await dataSource.GetPlanVersionAsync(id, ct)
            : await dataSource.GetPublishedFreeVersionAsync(utcNow, ct);
    }

    public async Task<Plan?> GetEffectivePlanAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default)
    {
        var version = await GetEffectivePlanVersionAsync(accountId, utcNow, ct);
        return version is null ? null : await dataSource.GetPlanAsync(version.PlanId, ct);
    }

    public async Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetPlanFeaturesAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default)
    {
        var version = await GetEffectivePlanVersionAsync(accountId, utcNow, ct);
        return version is null
            ? new Dictionary<string, PlanFeatureSetting>()
            : await dataSource.GetFeaturesAsync(version.Id, ct);
    }

    public async Task<PlanAccessDecision> CanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var plan = await GetEffectivePlanAsync(accountId, now, ct);
        var features = await GetPlanFeaturesAsync(accountId, now, ct);
        features.TryGetValue(featureCode, out var setting);
        var usage = await GetUsageAsync(accountId, featureCode, ct);
        var allowed = setting is not null && setting.Enabled != false &&
                      (setting.IsUnlimited || setting.Limit is null || usage < setting.Limit);
        return new PlanAccessDecision(allowed, featureCode, plan?.Code ?? "FREE", null, usage,
            setting?.IsUnlimited == true ? null : setting?.Limit,
            allowed ? string.Empty : "Este recurso não está disponível no seu plano ou o limite foi atingido.",
            allowed ? "Allowed" : setting is null ? "FeatureNotConfigured" : "PlanLimitReached");
    }

    public async Task<int?> GetLimitAsync(Guid accountId, string featureCode, CancellationToken ct = default)
    {
        var features = await GetPlanFeaturesAsync(accountId, DateTime.UtcNow, ct);
        return features.TryGetValue(featureCode, out var value) && !value.IsUnlimited ? value.Limit : null;
    }

    public Task<int> GetUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) =>
        dataSource.GetUsageAsync(accountId, featureCode, StartOfUtcMonth(DateTime.UtcNow), ct);

    public async Task<int?> GetRemainingUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default)
    {
        var limit = await GetLimitAsync(accountId, featureCode, ct);
        return limit is null ? null : Math.Max(0, limit.Value - await GetUsageAsync(accountId, featureCode, ct));
    }

    public async Task EnsureCanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default)
    {
        var decision = await CanUseAsync(accountId, featureCode, ct);
        if (!decision.IsAllowed) throw new PlanAccessDeniedException(decision);
    }

    public Task InvalidateAccountCacheAsync(Guid accountId, CancellationToken ct = default) => Task.CompletedTask;

    private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static DateTime StartOfUtcMonth(DateTime value) => new(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
}

public sealed class PlanAccessDeniedException(PlanAccessDecision decision) : InvalidOperationException(decision.UserMessage)
{
    public PlanAccessDecision Decision { get; } = decision;
}
