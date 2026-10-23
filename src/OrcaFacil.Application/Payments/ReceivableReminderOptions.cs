namespace OrcaFacil.Application.Payments;

/// <summary>Cadence for dunning the end client about an open receivable. No money moves here —
/// a reminder is a notification, not a charge.</summary>
public class ReceivableReminderOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>Days after the due date before the first reminder is allowed to go out.</summary>
    public int GraceDays { get; set; } = 3;
    /// <summary>Minimum days between two reminders of the same receivable.</summary>
    public int IntervalDays { get; set; } = 7;
    /// <summary>After this many reminders the receivable stops being dunned automatically.</summary>
    public int MaxReminders { get; set; } = 3;
}
