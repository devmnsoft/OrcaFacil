using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class BillingInvoiceItem : Entity
{
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class BillingPayment : Entity
{
    public Guid AccountId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public BillingPaymentMethod PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public BillingPaymentStatus Status { get; set; } = BillingPaymentStatus.Registered;
    public Guid RegisteredByUserId { get; set; }
    public string? ReversalReason { get; private set; }

    public void Reverse(string reason)
    {
        if (Status != BillingPaymentStatus.Registered) throw new InvalidOperationException("Somente pagamentos registrados podem ser revertidos.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Informe o motivo da reversão.", nameof(reason));
        Status = BillingPaymentStatus.Reversed;
        ReversalReason = reason.Trim();
        Touch();
    }
}

public sealed class SubscriptionChangeRequest : Entity
{
    public Guid AccountId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? CurrentPlanId { get; set; }
    public Guid? RequestedPlanId { get; set; }
    public SubscriptionChangeRequestType RequestType { get; set; }
    public SubscriptionChangeRequestStatus Status { get; set; } = SubscriptionChangeRequestStatus.Open;
    public string Reason { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class PlanAddon : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public decimal PriceAnnual { get; set; }
    public string LimitType { get; set; } = string.Empty;
    public long LimitIncrement { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AccountAddon : Entity
{
    public Guid AccountId { get; set; }
    public Guid AddonId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}

public sealed class AccountEntitlement : Entity
{
    public Guid AccountId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public long? LimitValue { get; set; }
    public string Source { get; set; } = "Plan";
}
