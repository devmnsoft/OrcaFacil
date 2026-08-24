using OrcaFacil.Application.Finance;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.UnitTests;

public sealed class AdvancedFinanceRulesTests
{
    [Fact]
    public void Partial_and_full_payment_update_real_balance()
    {
        var account = Guid.NewGuid();
        var payable = NewPayable(account, 100); AdvancedFinanceRules.Initialize(payable);
        var bank = new BankAccount { AccountId = account, IsActive = true, CurrentBalance = 500 };
        AdvancedFinanceRules.ApplyPayment(payable, NewPayment(account, payable.Id, bank.Id, 40), bank);
        Assert.Equal(PayableStatus.PartiallyPaid, payable.Status); Assert.Equal(60, payable.BalanceAmount);
        AdvancedFinanceRules.ApplyPayment(payable, NewPayment(account, payable.Id, bank.Id, 60), bank);
        Assert.Equal(PayableStatus.Paid, payable.Status); Assert.Equal(0, payable.BalanceAmount); Assert.Equal(400, bank.CurrentBalance);
    }

    [Fact]
    public void Payment_cannot_cross_accounts_or_exceed_balance()
    {
        var account = Guid.NewGuid(); var payable = NewPayable(account, 50); AdvancedFinanceRules.Initialize(payable);
        var bank = new BankAccount { AccountId = account, IsActive = true };
        Assert.Throws<UnauthorizedAccessException>(() => AdvancedFinanceRules.ApplyPayment(payable, NewPayment(Guid.NewGuid(), payable.Id, bank.Id, 10), bank));
        Assert.Throws<InvalidOperationException>(() => AdvancedFinanceRules.ApplyPayment(payable, NewPayment(account, payable.Id, bank.Id, 51), bank));
    }

    [Fact]
    public void Reversal_requires_reason_and_restores_bank_and_payable()
    {
        var account = Guid.NewGuid(); var payable = NewPayable(account, 50); AdvancedFinanceRules.Initialize(payable);
        var bank = new BankAccount { AccountId = account, IsActive = true, CurrentBalance = 100 };
        var payment = NewPayment(account, payable.Id, bank.Id, 50); AdvancedFinanceRules.ApplyPayment(payable, payment, bank);
        Assert.Throws<InvalidOperationException>(() => AdvancedFinanceRules.Reverse(payable, payment, bank, Guid.NewGuid(), ""));
        var reversal = AdvancedFinanceRules.Reverse(payable, payment, bank, Guid.NewGuid(), "lançamento duplicado");
        Assert.Equal(CashMovementType.Reversal, reversal.Type); Assert.Equal(50, payable.BalanceAmount); Assert.Equal(100, bank.CurrentBalance);
    }

    [Fact]
    public void Allocation_and_closed_period_are_strict()
    {
        AdvancedFinanceRules.ValidateAllocation([(60m, 60m), (40m, 40m)], 100m);
        Assert.Throws<InvalidOperationException>(() => AdvancedFinanceRules.ValidateAllocation([(90m, 90m)], 100m));
        var account = Guid.NewGuid(); var date = new DateTime(2026, 8, 10);
        Assert.Throws<InvalidOperationException>(() => AdvancedFinanceRules.EnsurePeriodOpen(account, date, [new FinancialPeriodClosing { AccountId = account, PeriodStart = new(2026,8,1), PeriodEnd = new(2026,8,31), IsClosed = true }]));
    }

    private static Payable NewPayable(Guid account, decimal amount) => new() { AccountId = account, Description = "Fornecedor", TotalAmount = amount, IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(10) };
    private static PayablePayment NewPayment(Guid account, Guid payable, Guid bank, decimal amount) => new() { AccountId = account, PayableId = payable, BankAccountId = bank, Amount = amount, PaymentDate = DateTime.UtcNow, IdempotencyKey = Guid.NewGuid().ToString(), PaymentMethod = "Pix" };
}
