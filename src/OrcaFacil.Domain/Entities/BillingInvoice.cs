using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Domain.Entities;
public class BillingInvoice : Entity
{
    public Guid AccountId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid PlanVersionId { get; set; }
    public BillingCycle Cycle { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Currency { get; set; } = "BRL";
    public BillingInvoiceStatus Status { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public void ApplyPayment(decimal value, DateTime paidAt)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "O pagamento deve ser positivo.");
        if (Status is BillingInvoiceStatus.Cancelled or BillingInvoiceStatus.Uncollectible) throw new InvalidOperationException("A cobrança não aceita pagamentos.");
        if (PaidAmount + value > Amount) throw new InvalidOperationException("O pagamento excede o saldo da cobrança.");
        PaidAmount += value;
        Status = PaidAmount == Amount ? BillingInvoiceStatus.Paid : BillingInvoiceStatus.PartiallyPaid;
        PaidAt = Status == BillingInvoiceStatus.Paid ? paidAt : null;
        Touch();
    }

    public void ReversePayment(decimal value)
    {
        if (value <= 0 || value > PaidAmount) throw new ArgumentOutOfRangeException(nameof(value));
        PaidAmount -= value;
        PaidAt = null;
        Status = PaidAmount == 0 ? (DueAt < DateTime.UtcNow ? BillingInvoiceStatus.Overdue : BillingInvoiceStatus.Issued) : BillingInvoiceStatus.PartiallyPaid;
        Touch();
    }
}
public class PlanOverride : Entity { public Guid AccountId { get; set; } public Guid PlanVersionId { get; set; } public string Reason { get; set; } = string.Empty; public DateTime StartsAt { get; set; } public DateTime EndsAt { get; set; } public Guid GrantedByUserId { get; set; } public DateTime? RevokedAt { get; set; } public Guid? RevokedByUserId { get; set; } public bool IsEffective(DateTime now) => RevokedAt is null && StartsAt <= now && EndsAt > now && !string.IsNullOrWhiteSpace(Reason); }
public class SubscriptionEvent : Entity { public Guid AccountId { get; set; } public Guid SubscriptionId { get; set; } public string EventType { get; set; } = string.Empty; public string? Details { get; set; } }
public class SupportAccessSession : Entity { public Guid PlatformUserId { get; set; } public Guid AccountId { get; set; } public string Reason { get; set; } = string.Empty; public SupportAccessMode Mode { get; set; } = SupportAccessMode.ReadOnly; public DateTime StartedAt { get; set; } = DateTime.UtcNow; public DateTime ExpiresAt { get; set; } public DateTime? EndedAt { get; set; } public bool IsValid(DateTime now) => EndedAt is null && StartedAt <= now && ExpiresAt > now && ExpiresAt <= StartedAt.AddMinutes(30) && !string.IsNullOrWhiteSpace(Reason); }
public class ActivityEvent : Entity { public Guid? AccountId { get; set; } public Guid? ActorUserId { get; set; } public string Action { get; set; } = string.Empty; public string? EntityType { get; set; } public Guid? EntityId { get; set; } public string Result { get; set; } = "Success"; public string? Summary { get; set; } }
