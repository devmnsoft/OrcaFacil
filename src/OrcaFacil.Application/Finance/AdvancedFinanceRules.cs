using OrcaFacil.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

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

    public static string BankTransactionFingerprint(Guid accountId, Guid bankAccountId, DateTime date, decimal amount, BankTransactionType type, string? reference)
    {
        RequireAccount(accountId);
        if (bankAccountId == Guid.Empty) throw new InvalidOperationException("Conta bancária é obrigatória.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var canonical = $"{accountId:N}|{bankAccountId:N}|{date:yyyy-MM-dd}|{amount:0.00}|{type}|{reference?.Trim().ToUpperInvariant()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static BankReconciliationMatch ConfirmReconciliation(BankReconciliationSession session, BankTransaction transaction, Guid? payablePaymentId, Guid? receivablePaymentId, Guid userId)
    {
        EnsureSameAccount(session.AccountId, transaction.AccountId);
        if (session.BankAccountId != transaction.BankAccountId) throw new InvalidOperationException("A transação não pertence à conta bancária da sessão.");
        if (session.Status != ReconciliationSessionStatus.Open) throw new InvalidOperationException("A sessão de conciliação não está aberta.");
        if (transaction.IsReconciled) throw new InvalidOperationException("A transação já está conciliada.");
        if (payablePaymentId.HasValue == receivablePaymentId.HasValue) throw new InvalidOperationException("Informe exatamente um lançamento financeiro para conciliar.");
        transaction.IsReconciled = true; transaction.Touch();
        return new BankReconciliationMatch { AccountId = session.AccountId, SessionId = session.Id, BankTransactionId = transaction.Id, PayablePaymentId = payablePaymentId, ReceivablePaymentId = receivablePaymentId, ConfirmedAt = DateTime.UtcNow, ConfirmedByUserId = userId };
    }

    public static void UndoReconciliation(BankReconciliationMatch match, BankTransaction transaction, Guid userId, string reason)
    {
        EnsureSameAccount(match.AccountId, transaction.AccountId);
        if (match.BankTransactionId != transaction.Id || match.ReversedAt.HasValue) throw new InvalidOperationException("Conciliação inválida ou já desfeita.");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Desfazer a conciliação exige motivo.");
        match.ReversedAt = DateTime.UtcNow; match.ReversedByUserId = userId; match.ReversalReason = reason.Trim(); match.Touch();
        transaction.IsReconciled = false; transaction.Touch();
    }
}
