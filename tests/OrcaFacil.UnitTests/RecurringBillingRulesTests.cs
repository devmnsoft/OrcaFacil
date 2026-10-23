using OrcaFacil.Application.Contracts;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>Lifecycle of a client's recurring subscription. Before these rules were applied by a worker,
/// NextBillingDate never advanced and Overdue/Expired were never written.</summary>
public sealed class RecurringBillingRulesTests
{
    [Theory]
    [InlineData(RecurrencePeriod.Monthly, 1)]
    [InlineData(RecurrencePeriod.Bimonthly, 2)]
    [InlineData(RecurrencePeriod.Quarterly, 3)]
    [InlineData(RecurrencePeriod.Semiannual, 6)]
    [InlineData(RecurrencePeriod.Annual, 12)]
    public void Period_maps_to_months(RecurrencePeriod period, int months) =>
        Assert.Equal(months, RecurringBillingRules.PeriodMonths(period, null));

    [Fact]
    public void Custom_period_uses_declared_months()
    {
        Assert.Equal(5, RecurringBillingRules.PeriodMonths(RecurrencePeriod.Custom, 5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-2)]
    public void Custom_period_without_positive_months_is_rejected(int? months) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RecurringBillingRules.PeriodMonths(RecurrencePeriod.Custom, months));

    [Fact]
    public void Monthly_advance_keeps_the_due_day()
    {
        var next = RecurringBillingRules.AdvanceDue(new DateOnly(2026, 1, 10), RecurrencePeriod.Monthly, null, 10);
        Assert.Equal(new DateOnly(2026, 2, 10), next);
    }

    [Fact]
    public void Advance_clamps_due_day_to_month_end()
    {
        // Due day 31 in January advances to February, which has 28 days in 2026.
        var next = RecurringBillingRules.AdvanceDue(new DateOnly(2026, 1, 31), RecurrencePeriod.Monthly, null, 31);
        Assert.Equal(new DateOnly(2026, 2, 28), next);
    }

    [Fact]
    public void Quarterly_advance_crosses_the_year()
    {
        var next = RecurringBillingRules.AdvanceDue(new DateOnly(2026, 11, 5), RecurrencePeriod.Quarterly, null, 5);
        Assert.Equal(new DateOnly(2027, 2, 5), next);
    }

    [Fact]
    public void Competence_is_the_first_day_of_the_due_month() =>
        Assert.Equal(new DateOnly(2026, 3, 1), RecurringBillingRules.CompetenceOf(new DateOnly(2026, 3, 17)));

    [Fact]
    public void Active_contract_generates_when_due_and_inside_the_term()
    {
        Assert.True(RecurringBillingRules.ShouldGenerate(ContractStatus.Active, new DateOnly(2026, 5, 10), new DateOnly(2027, 1, 1), new DateOnly(2026, 5, 10)));
    }

    [Fact]
    public void Nothing_is_generated_before_the_due_date()
    {
        Assert.False(RecurringBillingRules.ShouldGenerate(ContractStatus.Active, new DateOnly(2026, 5, 11), null, new DateOnly(2026, 5, 10)));
    }

    [Fact]
    public void Nothing_is_generated_after_the_term_ends()
    {
        Assert.False(RecurringBillingRules.ShouldGenerate(ContractStatus.Active, new DateOnly(2026, 6, 10), new DateOnly(2026, 5, 31), new DateOnly(2026, 6, 10)));
    }

    [Theory]
    [InlineData(ContractStatus.Paused)]
    [InlineData(ContractStatus.Suspended)]
    [InlineData(ContractStatus.Canceled)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Draft)]
    public void Inactive_contract_never_generates(ContractStatus status) =>
        Assert.False(RecurringBillingRules.ShouldGenerate(status, new DateOnly(2026, 5, 1), null, new DateOnly(2026, 5, 10)));

    [Fact]
    public void Pending_charge_past_due_is_overdue()
    {
        Assert.True(RecurringBillingRules.ShouldMarkOverdue(RecurringPaymentStatus.Pending, new DateOnly(2026, 5, 9), new DateOnly(2026, 5, 10)));
    }

    [Fact]
    public void Charge_due_today_is_not_overdue_yet()
    {
        Assert.False(RecurringBillingRules.ShouldMarkOverdue(RecurringPaymentStatus.Pending, new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 10)));
    }

    [Theory]
    [InlineData(RecurringPaymentStatus.Paid)]
    [InlineData(RecurringPaymentStatus.Canceled)]
    [InlineData(RecurringPaymentStatus.Reversed)]
    [InlineData(RecurringPaymentStatus.Overdue)]
    public void Settled_or_already_overdue_charge_is_not_marked_again(RecurringPaymentStatus status) =>
        Assert.False(RecurringBillingRules.ShouldMarkOverdue(status, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 10)));

    [Fact]
    public void Active_contract_past_its_end_expires()
    {
        Assert.True(RecurringBillingRules.ShouldExpire(ContractStatus.Active, new DateOnly(2026, 5, 9), new DateOnly(2026, 5, 10)));
    }

    [Fact]
    public void Contract_ending_today_is_still_running()
    {
        Assert.False(RecurringBillingRules.ShouldExpire(ContractStatus.Active, new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 10)));
    }

    [Theory]
    [InlineData(ContractStatus.Paused)]
    [InlineData(ContractStatus.Canceled)]
    [InlineData(ContractStatus.Suspended)]
    public void Non_active_contract_is_not_expired_by_the_term(ContractStatus status) =>
        Assert.False(RecurringBillingRules.ShouldExpire(status, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 10)));
}
