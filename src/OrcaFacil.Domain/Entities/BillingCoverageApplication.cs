using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public sealed class BillingCoverageApplication : Entity
{
    public Guid AccountId { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string ExternalPaymentId { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public int CycleMonths { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
}
