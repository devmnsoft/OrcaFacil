using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum SaasModuleSubscriptionStatus { PendingApproval, Trial, Active, Paused, Suspended, Cancelled, Blocked }
public enum SaasBillingPeriod { Monthly, Annual }
public enum SaasModuleGrantSource { Subscription, Trial, Manual }

public sealed class SaasModule : Entity
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IconKey { get; set; } = "grid";
    public string MenuGroup { get; set; } = string.Empty;
    public string RoutePrefix { get; set; } = string.Empty;
    public string RequiredPermissionCode { get; set; } = string.Empty;
    public decimal BaseMonthlyPrice { get; set; }
    public decimal BaseAnnualPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPublic { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public void SoftDelete(Guid actorId) { MarkAsDeleted(); DeletedAt = DateTime.UtcNow; DeletedBy = actorId; IsActive = false; }
}

public sealed class SaasModuleFeature : Entity
{
    public Guid ModuleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RequiredPermissionCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class SaasModulePrice : Entity
{
    public Guid ModuleId { get; set; }
    public SaasBillingPeriod BillingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AccountModuleSubscription : Entity
{
    public Guid AccountId { get; set; }
    public Guid ModuleId { get; set; }
    public SaasModuleSubscriptionStatus Status { get; private set; } = SaasModuleSubscriptionStatus.PendingApproval;
    public SaasBillingPeriod BillingPeriod { get; set; } = SaasBillingPeriod.Monthly;
    public decimal ContractedPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime? StartsAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? ManualGrantEndsAt { get; set; }
    public string? ManualGrantReason { get; set; }
    public DateTime? SuspendedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    public bool GrantsAccess(DateTime now) => !IsDeleted &&
        Status is SaasModuleSubscriptionStatus.Active or SaasModuleSubscriptionStatus.Trial &&
        (TrialEndsAt is null || TrialEndsAt > now) && (ManualGrantEndsAt is null || ManualGrantEndsAt > now);
    public void Activate(Guid actorId) { Status = SaasModuleSubscriptionStatus.Active; StartsAt ??= DateTime.UtcNow; SuspendedAt = null; ChangedByUserId = actorId; Touch(); }
    public void StartTrial(Guid actorId, DateTime endsAt) { if (endsAt <= DateTime.UtcNow) throw new ArgumentException("O fim do trial deve estar no futuro."); Status = SaasModuleSubscriptionStatus.Trial; TrialEndsAt = endsAt; StartsAt ??= DateTime.UtcNow; ChangedByUserId = actorId; Touch(); }
    public void Suspend(Guid actorId) { Status = SaasModuleSubscriptionStatus.Suspended; SuspendedAt = DateTime.UtcNow; ChangedByUserId = actorId; Touch(); }
    public void Cancel(Guid actorId) { Status = SaasModuleSubscriptionStatus.Cancelled; CancelledAt = DateTime.UtcNow; ChangedByUserId = actorId; Touch(); }
}

public sealed class AccountModuleEntitlement : Entity
{
    public Guid AccountId { get; set; }
    public Guid ModuleId { get; set; }
    public string? FeatureCode { get; set; }
    public bool IsEnabled { get; set; } = true;
    public SaasModuleGrantSource Source { get; set; } = SaasModuleGrantSource.Subscription;
    public DateTime? ValidUntil { get; set; }
    public string? Reason { get; set; }
    public Guid GrantedByUserId { get; set; }
}

public sealed class AccountModuleFeatureLimit : Entity
{
    public Guid AccountId { get; set; }
    public Guid ModuleId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public long LimitValue { get; set; }
    public string Period { get; set; } = "Monthly";
}

public sealed class AccountModuleUsageEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string? FeatureCode { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public sealed class AccountModuleUsageSnapshot : Entity
{
    public Guid AccountId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public long EventCount { get; set; }
    public int ActiveUsers { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public sealed class AccountModuleAuditLog : Entity
{
    public Guid? AccountId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? ModuleCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class AccountMemberProfile : Entity
{
    public Guid AccountId { get; set; }
    public Guid AccountMemberId { get; set; }
    public Guid RoleId { get; set; }
    public Guid AssignedByUserId { get; set; }
}
