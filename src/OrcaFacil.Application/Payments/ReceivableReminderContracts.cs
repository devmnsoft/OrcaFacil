namespace OrcaFacil.Application.Payments;

public sealed record ReceivableReminderRunResult(int Accounts, int Reminded, int Emails, int WhatsApps);

/// <summary>Cross-account dunning for open receivables. Notifies the end client by email and/or WhatsApp;
/// never moves money, changes a payment, or touches the amount due.</summary>
public interface IReceivableReminderService
{
    Task<ReceivableReminderRunResult> RunAsync(DateOnly today, CancellationToken ct = default);
}
