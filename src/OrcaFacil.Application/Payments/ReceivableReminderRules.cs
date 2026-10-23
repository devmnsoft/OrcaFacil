using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Payments;

/// <summary>Pure cadence rules for dunning an end client about an open receivable. Free of the DbContext so
/// the reminder decisions are unit-testable. A reminder moves no money — it only tells the client what is owed.</summary>
public static class ReceivableReminderRules
{
    /// <summary>A receivable is open while it is unsettled, uncanceled and still carries balance.</summary>
    public static bool IsOpen(FinancialEntryStatus status, decimal amount, decimal paid) =>
        (status == FinancialEntryStatus.Pending || status == FinancialEntryStatus.PartiallyPaid)
        && amount - paid > 0m;

    /// <summary>What the client still owes. Reminders talk about this, never about the gross amount.</summary>
    public static decimal OpenBalance(decimal amount, decimal paid) => Math.Max(0m, amount - paid);

    public static int DaysBetween(DateOnly from, DateOnly to) =>
        (new DateTime(to.Year, to.Month, to.Day) - new DateTime(from.Year, from.Month, from.Day)).Days;

    public static int DaysPastDue(DateOnly due, DateOnly today) => DaysBetween(due, today);

    /// <summary>The first reminder may leave only after the grace window closes.</summary>
    public static bool GraceElapsed(DateOnly due, DateOnly today, int graceDays) =>
        graceDays >= 0 && DaysPastDue(due, today) >= graceDays;

    /// <summary>Cadence: nothing twice close together. A null last reminder means the first is due once grace elapses.</summary>
    public static bool DueForNextReminder(DateOnly? lastReminderAt, DateOnly today, int intervalDays) =>
        lastReminderAt is null || intervalDays <= 0 || DaysBetween(lastReminderAt.Value, today) >= intervalDays;

    /// <summary>Bounded dunning: after the cap, automatic reminders stop (a human can always follow up).</summary>
    public static bool UnderLimit(int reminderCount, int maxReminders) =>
        maxReminders <= 0 || reminderCount < maxReminders;

    public static bool ShouldRemind(FinancialEntryStatus status, decimal amount, decimal paid,
        DateOnly due, DateOnly today, DateOnly? lastReminderAt, int reminderCount,
        int graceDays, int intervalDays, int maxReminders) =>
        IsOpen(status, amount, paid)
        && GraceElapsed(due, today, graceDays)
        && DueForNextReminder(lastReminderAt, today, intervalDays)
        && UnderLimit(reminderCount, maxReminders);
}
