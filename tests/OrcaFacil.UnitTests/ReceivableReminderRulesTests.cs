using OrcaFacil.Application.Payments;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>Cadence of dunning the end client about an open receivable. Before these rules, nothing reminded
/// the client at all; these guard the grace window, the interval and the cap so a reminder never spams.</summary>
public sealed class ReceivableReminderRulesTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Theory]
    [InlineData(FinancialEntryStatus.Pending, 100, 0, true)]
    [InlineData(FinancialEntryStatus.PartiallyPaid, 100, 40, true)]
    [InlineData(FinancialEntryStatus.Paid, 100, 100, false)]
    [InlineData(FinancialEntryStatus.Canceled, 100, 0, false)]
    [InlineData(FinancialEntryStatus.Overdue, 100, 0, false)] // overdue by state alone does not mean open to us
    public void Open_receivable_requires_unsettled_status_and_balance(FinancialEntryStatus status, decimal amount, decimal paid, bool expected) =>
        Assert.Equal(expected, ReceivableReminderRules.IsOpen(status, amount, paid));

    [Fact]
    public void Overdue_status_entry_still_reminds_when_balance_is_open() =>
        // A WorkOrder entry can be past due yet still Pending in status; dunning is driven by due date + balance.
        Assert.True(ReceivableReminderRules.IsOpen(FinancialEntryStatus.Pending, 50, 10));

    [Fact]
    public void Open_balance_ignores_paid_portion() =>
        Assert.Equal(60m, ReceivableReminderRules.OpenBalance(100, 40));

    [Fact]
    public void Open_balance_never_goes_negative() =>
        Assert.Equal(0m, ReceivableReminderRules.OpenBalance(40, 100));

    [Fact]
    public void Grace_window_blocks_the_first_reminder()
    {
        Assert.False(ReceivableReminderRules.GraceElapsed(D(2026, 5, 1), D(2026, 5, 3), 3));
        Assert.True(ReceivableReminderRules.GraceElapsed(D(2026, 5, 1), D(2026, 5, 4), 3));
    }

    [Fact]
    public void Grace_never_reminds_before_the_due_date()
    {
        // One day before the due date, even with zero grace, there is nothing overdue yet.
        Assert.False(ReceivableReminderRules.GraceElapsed(D(2026, 5, 1), D(2026, 4, 30), 0));
        // On and after the due date a zero-grace receivable may be dunned.
        Assert.True(ReceivableReminderRules.GraceElapsed(D(2026, 5, 1), D(2026, 5, 1), 0));
    }

    [Fact]
    public void Cadence_respects_the_interval_between_reminders()
    {
        Assert.True(ReceivableReminderRules.DueForNextReminder(null, D(2026, 5, 10), 7));
        Assert.True(ReceivableReminderRules.DueForNextReminder(D(2026, 5, 1), D(2026, 5, 8), 7));
        Assert.False(ReceivableReminderRules.DueForNextReminder(D(2026, 5, 1), D(2026, 5, 7), 7));
        Assert.False(ReceivableReminderRules.DueForNextReminder(D(2026, 5, 7), D(2026, 5, 7), 7));
    }

    [Fact]
    public void Cap_stops_further_reminders()
    {
        Assert.True(ReceivableReminderRules.UnderLimit(0, 3));
        Assert.True(ReceivableReminderRules.UnderLimit(2, 3));
        Assert.False(ReceivableReminderRules.UnderLimit(3, 3));
    }

    [Fact]
    public void Full_decision_requires_every_condition()
    {
        var today = D(2026, 5, 20);
        // open + grace elapsed + first reminder + under limit
        Assert.True(ReceivableReminderRules.ShouldRemind(FinancialEntryStatus.Pending, 100, 0,
            D(2026, 5, 1), today, null, 0, graceDays: 3, intervalDays: 7, maxReminders: 3));
        // already hit the cap
        Assert.False(ReceivableReminderRules.ShouldRemind(FinancialEntryStatus.Pending, 100, 0,
            D(2026, 5, 1), today, D(2026, 5, 1), 3, 3, 7, 3));
        // still within interval of the last reminder
        Assert.False(ReceivableReminderRules.ShouldRemind(FinancialEntryStatus.Pending, 100, 0,
            D(2026, 5, 1), D(2026, 5, 13), D(2026, 5, 9), 1, 3, 7, 3));
        // not past grace yet
        Assert.False(ReceivableReminderRules.ShouldRemind(FinancialEntryStatus.Pending, 100, 0,
            D(2026, 5, 18), D(2026, 5, 20), null, 0, 3, 7, 3));
        // fully paid
        Assert.False(ReceivableReminderRules.ShouldRemind(FinancialEntryStatus.Paid, 100, 100,
            D(2026, 5, 1), today, null, 0, 3, 7, 3));
    }

    [Fact]
    public void Days_past_due_counts_civil_days() =>
        Assert.Equal(19, ReceivableReminderRules.DaysPastDue(D(2026, 5, 1), D(2026, 5, 20)));
}
