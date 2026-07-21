using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Subscription : Entity
{
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
}
