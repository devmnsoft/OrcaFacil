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
        if (await dataSource.GetAccountStatusAsync(accountId, ct) != AccountStatus.Active)
            return null;

        var activeOverride = await dataSource.GetActiveOverrideAsync(accountId, utcNow, ct);
        if (activeOverride is not null)
            return await dataSource.GetPlanVersionAsync(activeOverride.PlanVersionId, ct);

        var subscription = await dataSource.GetSubscriptionAsync(accountId, ct);
        if (subscription is null) return await dataSource.GetPublishedFreeVersionAsync(utcNow, ct);

        var dueAt = subscription.PaidThroughAt ?? subscription.ExpiresAt ?? subscription.NextDueAt;
        var selectedVersion = subscription.SelectedPlanVersionId is Guid selectedId
            ? await dataSource.GetPlanVersionAsync(selectedId, ct)
            : null;
        var graceEndsAt = dueAt?.AddDays(Math.Max(0, selectedVersion?.GracePeriodDays ?? 0));
        var paidAccess = subscription.Status is not (SubscriptionStatus.Free or SubscriptionStatus.Cancelled or SubscriptionStatus.Suspended)
                         && (subscription.ManualReleaseUntil > utcNow || dueAt is null || graceEndsAt >= utcNow);
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
        var accountStatus = await dataSource.GetAccountStatusAsync(accountId, ct);
        if (accountStatus != AccountStatus.Active)
            return new(false, featureCode, "NONE", null, 0, null,
                accountStatus == AccountStatus.Blocked ? "Esta conta está bloqueada." : "Esta conta não está ativa.",
                accountStatus == AccountStatus.Blocked ? "AccountBlocked" : "AccountInactive");

        var plan = await GetEffectivePlanAsync(accountId, now, ct);
        var features = await GetPlanFeaturesAsync(accountId, now, ct);
        features.TryGetValue(featureCode, out var setting);
        var usage = await GetUsageAsync(accountId, featureCode, ct);
        var allowed = setting is not null && setting.Enabled != false &&
                      (setting.IsUnlimited || setting.Limit is null || usage < setting.Limit);
        var requiredPlanCode = allowed ? null : await FindRequiredPlanCodeAsync(featureCode, usage, now, ct);
        var reason = setting is null ? "FeatureNotConfigured"
            : setting.Enabled == false ? "FeatureNotIncluded"
            : !setting.IsUnlimited && setting.Limit is int limit && usage >= limit ? "PlanLimitReached"
            : "FeatureNotIncluded";
        var message = reason switch
        {
            "FeatureNotConfigured" => "Este recurso não está configurado. Tente novamente mais tarde.",
            "PlanLimitReached" => "Você atingiu o limite deste recurso no plano atual.",
            _ => "Este recurso não está incluído no seu plano atual."
        };
        return new PlanAccessDecision(allowed, featureCode, plan?.Code ?? "FREE", requiredPlanCode, usage,
            setting?.IsUnlimited == true ? null : setting?.Limit,
            allowed ? string.Empty : message, allowed ? "Allowed" : reason);
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

    private async Task<string?> FindRequiredPlanCodeAsync(string featureCode, int usage, DateTime utcNow, CancellationToken ct)
    {
        var candidates = await dataSource.GetPublicPlanCandidatesAsync(featureCode, utcNow, ct);
        return candidates
            .Where(x => x.Setting.Enabled != false &&
                        (x.Setting.IsUnlimited || x.Setting.Limit is null || usage < x.Setting.Limit))
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.PlanCode)
            .FirstOrDefault();
    }
}

public sealed class PlanAccessDeniedException(PlanAccessDecision decision) : InvalidOperationException(decision.UserMessage)
{
    public PlanAccessDecision Decision { get; } = decision;
}
