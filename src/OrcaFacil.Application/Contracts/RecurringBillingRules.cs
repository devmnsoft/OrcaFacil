using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Contracts;

/// <summary>Pure lifecycle rules for a client's recurring subscription. Kept free of the DbContext so the
/// billing decisions are unit-testable. The background service applies these; nothing else changes state.</summary>
public static class RecurringBillingRules
{
    public static int PeriodMonths(RecurrencePeriod period, int? customMonths) => period switch
    {
        RecurrencePeriod.Monthly => 1,
        RecurrencePeriod.Bimonthly => 2,
        RecurrencePeriod.Quarterly => 3,
        RecurrencePeriod.Semiannual => 6,
        RecurrencePeriod.Annual => 12,
        RecurrencePeriod.Custom when customMonths is >= 1 => customMonths.Value,
        RecurrencePeriod.Custom => throw new ArgumentOutOfRangeException(nameof(customMonths), "Período personalizado precisa de meses maiores que zero."),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    /// <summary>Next due date after <paramref name="due"/>, keeping the contract's due day and clamping to month end.</summary>
    public static DateOnly AdvanceDue(DateOnly due, RecurrencePeriod period, int? customMonths, int dueDay)
    {
        var target = due.AddMonths(PeriodMonths(period, customMonths));
        var day = Math.Min(Math.Clamp(dueDay, 1, 31), DateTime.DaysInMonth(target.Year, target.Month));
        return new DateOnly(target.Year, target.Month, day);
    }

    /// <summary>The competence (first day of the month) a due date belongs to.</summary>
    public static DateOnly CompetenceOf(DateOnly due) => new(due.Year, due.Month, 1);

    /// <summary>A period is generated only for an active contract whose due date has arrived and still falls inside the term.</summary>
    public static bool ShouldGenerate(ContractStatus status, DateOnly? nextBilling, DateOnly? endDate, DateOnly today) =>
        ContractRecurrenceRules.CanGenerate(status)
        && nextBilling is { } due && due <= today
        && (endDate is null || due <= endDate.Value);

    /// <summary>A pending charge whose due date has passed is overdue. Paid, canceled and future charges are left alone.</summary>
    public static bool ShouldMarkOverdue(RecurringPaymentStatus status, DateOnly dueDate, DateOnly today) =>
        status == RecurringPaymentStatus.Pending && dueDate < today;

    /// <summary>An active contract whose term ended is expired. Suspended, paused or canceled contracts are not touched.</summary>
    public static bool ShouldExpire(ContractStatus status, DateOnly? endDate, DateOnly today) =>
        status == ContractStatus.Active && endDate is { } end && end < today;
}
