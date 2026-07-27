using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Subscription : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? SelectedPlanVersionId { get; set; }
    public Guid? EffectivePlanVersionId { get; set; }
    public decimal PriceAtActivation { get; set; }
    public DateTime? PaidThroughAt { get; set; }
    public DateTime? NextDueAt { get; set; }
    public DateTime? PastDueSince { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? ManualReleaseUntil { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "Manual";
    public SubscriptionStatus Status { get; set; }
    public PlanType Plan { get; set; }
    public string? BillingCycle { get; set; }
    public decimal Amount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public string? ExternalCustomerId { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public DateTime? TrialStartedAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool TrialUsed { get; set; }
    public TrialStatus TrialStatus { get; set; } = TrialStatus.NotStarted;

    public bool HasActiveTrial(DateTime utcNow) => TrialStatus == TrialStatus.Active && TrialEndsAt > utcNow;
}
