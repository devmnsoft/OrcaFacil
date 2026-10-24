namespace OrcaFacil.Application.Contracts;

public sealed record RecurringBillingRunResult(int Generated, int Overdue, int Expired);

/// <summary>Cross-account billing lifecycle for recurring client contracts, driven by the background worker.</summary>
public interface IRecurringContractBillingService
{
    Task<RecurringBillingRunResult> RunAsync(DateOnly today, CancellationToken ct = default);
}
