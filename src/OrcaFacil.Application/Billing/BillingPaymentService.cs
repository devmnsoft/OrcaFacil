using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Billing;

public sealed class BillingPaymentService(IRepository<BillingInvoice> invoices, IRepository<BillingPayment> payments, IUnitOfWork unitOfWork)
{
    public async Task<BillingPayment> RegisterAsync(Guid accountId, Guid invoiceId, decimal amount, DateTime paymentDate,
        BillingPaymentMethod method, string? reference, Guid actorId, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "O pagamento deve ser positivo.");
        var invoice = await invoices.GetAsync(invoiceId, ct) ?? throw new KeyNotFoundException("Cobrança não encontrada.");
        if (invoice.AccountId != accountId) throw new InvalidOperationException("A cobrança não pertence à conta informada.");
        invoice.ApplyPayment(amount, paymentDate);
        var payment = new BillingPayment { AccountId = accountId, InvoiceId = invoiceId, Amount = amount, PaymentDate = paymentDate, PaymentMethod = method, Reference = reference?.Trim(), RegisteredByUserId = actorId };
        await payments.AddAsync(payment, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return payment;
    }

    public async Task ReverseAsync(Guid paymentId, string reason, CancellationToken ct = default)
    {
        var payment = await payments.GetAsync(paymentId, ct) ?? throw new KeyNotFoundException("Pagamento não encontrado.");
        var invoice = await invoices.GetAsync(payment.InvoiceId, ct) ?? throw new InvalidOperationException("Cobrança do pagamento não encontrada.");
        payment.Reverse(reason);
        invoice.ReversePayment(payment.Amount);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
