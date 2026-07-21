using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Payment : Entity
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "Manual";
    public PaymentStatus Status { get; set; }
    public PlanType Plan { get; set; }
    public string? BillingCycle { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? ExternalPaymentId { get; set; }
    public string? ExternalPreferenceId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PayerEmail { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
