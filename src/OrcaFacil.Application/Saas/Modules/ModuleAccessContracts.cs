namespace OrcaFacil.Application.Saas.Modules;

public sealed record ModuleAccessDecision(bool Allowed, string ModuleCode, string? Reason = null)
{
    public static ModuleAccessDecision Denied(string module, string reason) => new(false, module, reason);
}

public interface IModuleEntitlementService
{
    Task<ModuleAccessDecision> CheckAsync(Guid? accountId, string moduleCode, bool isSuperAdmin, CancellationToken ct = default);
}

public interface IModuleAccessService
{
    Task<ModuleAccessDecision> CheckAsync(Guid? accountId, string moduleCode, string? featureCode,
        bool hasPermission, bool isSuperAdmin, CancellationToken ct = default);
}

public interface IFeatureAccessService
{
    Task<bool> HasAccessAsync(Guid accountId, string moduleCode, string featureCode, CancellationToken ct = default);
}

public interface IAccountModuleSubscriptionService
{
    Task RequestAsync(Guid accountId, string moduleCode, string billingPeriod, Guid actorUserId, CancellationToken ct = default);
    Task ActivateAsync(Guid accountId, Guid moduleId, decimal contractedPrice, Guid actorUserId, CancellationToken ct = default);
    Task SuspendAsync(Guid accountId, Guid moduleId, Guid actorUserId, string reason, CancellationToken ct = default);
    Task StartTrialAsync(Guid accountId, Guid moduleId, DateTime endsAt, Guid actorUserId, CancellationToken ct = default);
    Task GrantManualAsync(Guid accountId, Guid moduleId, DateTime endsAt, string reason, Guid actorUserId, CancellationToken ct = default);
    Task UpdatePriceAsync(Guid accountId, Guid moduleId, decimal contractedPrice, decimal discountAmount, Guid actorUserId, CancellationToken ct = default);
    Task CancelAsync(Guid accountId, Guid moduleId, Guid actorUserId, string reason, CancellationToken ct = default);
}

public interface IModuleUsageTracker
{
    Task TrackAsync(Guid accountId, Guid? userId, string moduleCode, string eventCode, string? featureCode,
        string? route, string correlationId, CancellationToken ct = default);
}
