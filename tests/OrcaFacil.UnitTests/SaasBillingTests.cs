using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class SaasBillingTests
{
    [Fact]
    public void Partial_and_full_payment_update_invoice_without_approving_implicitly()
    {
        var invoice = new BillingInvoice { AccountId = Guid.NewGuid(), Amount = 100, DueAt = DateTime.UtcNow.AddDays(2), Status = BillingInvoiceStatus.Issued };
        invoice.ApplyPayment(40, DateTime.UtcNow);
        Assert.Equal(BillingInvoiceStatus.PartiallyPaid, invoice.Status);
        Assert.Equal(40, invoice.PaidAmount);
        invoice.ApplyPayment(60, DateTime.UtcNow);
        Assert.Equal(BillingInvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAt);
    }

    [Fact]
    public void Payment_cannot_exceed_balance()
    {
        var invoice = new BillingInvoice { Amount = 50, DueAt = DateTime.UtcNow, Status = BillingInvoiceStatus.Issued };
        Assert.Throws<InvalidOperationException>(() => invoice.ApplyPayment(51, DateTime.UtcNow));
    }

    [Fact]
    public void Reversal_requires_reason_and_reopens_invoice()
    {
        var invoice = new BillingInvoice { Amount = 80, DueAt = DateTime.UtcNow.AddDays(1), Status = BillingInvoiceStatus.Issued };
        invoice.ApplyPayment(80, DateTime.UtcNow);
        var payment = new BillingPayment { Amount = 80, Status = BillingPaymentStatus.Registered };
        Assert.Throws<ArgumentException>(() => payment.Reverse(" "));
        payment.Reverse("Transferência estornada pelo banco");
        invoice.ReversePayment(80);
        Assert.Equal(BillingPaymentStatus.Reversed, payment.Status);
        Assert.Equal(BillingInvoiceStatus.Issued, invoice.Status);
        Assert.Equal(0, invoice.PaidAmount);
    }
}
