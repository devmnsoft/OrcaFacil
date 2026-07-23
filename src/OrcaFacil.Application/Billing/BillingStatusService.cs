namespace OrcaFacil.Application.Billing;
public class BillingStatusService
{
    public Task<int> SyncOverdueSubscriptionsAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> SuspendPastDueBenefitsAsync(CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> RestorePaidBenefitsAsync(CancellationToken ct = default) => Task.FromResult(0);
}
public class BillingOptions { public int GracePeriodDays { get; set; } = 3; public int SuspendAfterDays { get; set; } = 5; }
