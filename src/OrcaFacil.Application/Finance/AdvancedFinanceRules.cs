using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Finance;

public static class AdvancedFinanceRules
{
    public const string FiscalNotConfiguredMessage = "A emissão fiscal automática não está configurada. Este controle registra apenas a solicitação e o acompanhamento interno.";

    public static void Initialize(Payable payable)
    {
        RequireAccount(payable.AccountId);
        if (payable.TotalAmount <= 0) throw new ArgumentOutOfRangeException(nameof(payable.TotalAmount), "O valor deve ser maior que zero.");
        payable.PaidAmount = 0;
        payable.BalanceAmount = payable.TotalAmount;
        payable.Status = payable.DueDate.Date < DateTime.UtcNow.Date ? PayableStatus.Overdue : PayableStatus.Open;
    }

    public static void ApplyPayment(Payable payable, PayablePayment payment, BankAccount bankAccount, bool allowOverpayment = false)
    {
        EnsureSameAccount(payable.AccountId, payment.AccountId);
        EnsureSameAccount(payable.AccountId, bankAccount.AccountId);
        if (payment.PayableId != payable.Id) throw new InvalidOperationException("O pagamento não pertence ao título informado.");
        if (!bankAccount.IsActive || bankAccount.IsDeleted) throw new InvalidOperationException("A conta bancária está inativa.");
        if (payment.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(payment.Amount));
        if (payment.Amount > payable.BalanceAmount && !allowOverpayment) throw new InvalidOperationException("O pagamento excede o saldo do título.");
        var applied = Math.Min(payment.Amount, payable.BalanceAmount);
        payable.PaidAmount += applied;
        payable.BalanceAmount -= applied;
        payable.Status = payable.BalanceAmount == 0 ? PayableStatus.Paid : PayableStatus.PartiallyPaid;
        bankAccount.CurrentBalance -= payment.Amount;
        payable.Touch(); bankAccount.Touch();
    }

    public static CashMovement Reverse(Payable payable, PayablePayment payment, BankAccount bankAccount, Guid userId, string reason)
    {
        EnsureSameAccount(payable.AccountId, payment.AccountId); EnsureSameAccount(payable.AccountId, bankAccount.AccountId);
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Estorno exige motivo.");
        if (payment.Status == PayablePaymentStatus.Reversed) throw new InvalidOperationException("Pagamento já estornado.");
        payment.Status = PayablePaymentStatus.Reversed;
        payable.PaidAmount -= payment.Amount; payable.BalanceAmount += payment.Amount;
        payable.Status = payable.DueDate.Date < DateTime.UtcNow.Date ? PayableStatus.Overdue : PayableStatus.Open;
        bankAccount.CurrentBalance += payment.Amount;
        return new CashMovement { AccountId = payable.AccountId, BankAccountId = bankAccount.Id, Type = CashMovementType.Reversal, Amount = payment.Amount, MovementDate = DateTime.UtcNow, Description = $"Estorno: {payable.Description}", Reason = reason, ReversesMovementId = payment.CashMovementId, CreatedByUserId = userId, IdempotencyKey = $"payable-payment-reversal:{payment.Id}" };
    }

    public static void ValidateAllocation(IEnumerable<(decimal Percent, decimal Amount)> allocations, decimal total)
    {
        var values = allocations.ToArray();
        if (values.Any(x => x.Percent < 0 || x.Amount < 0) || values.Sum(x => x.Percent) != 100m || values.Sum(x => x.Amount) != total)
            throw new InvalidOperationException("O rateio deve fechar em 100% e no valor integral do lançamento.");
    }

    public static void EnsurePeriodOpen(Guid accountId, DateTime date, IEnumerable<FinancialPeriodClosing> closings)
    { if (closings.Any(x => x.AccountId == accountId && x.IsClosed && date.Date >= x.PeriodStart.Date && date.Date <= x.PeriodEnd.Date)) throw new InvalidOperationException("O período financeiro está fechado."); }
    public static void EnsureSameAccount(Guid expected, Guid actual) { RequireAccount(expected); if (expected != actual) throw new UnauthorizedAccessException("O recurso não pertence à conta ativa."); }
    private static void RequireAccount(Guid accountId) { if (accountId == Guid.Empty) throw new InvalidOperationException("AccountId é obrigatório."); }
}
