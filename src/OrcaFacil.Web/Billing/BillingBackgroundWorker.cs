using Microsoft.Extensions.Options;
using OrcaFacil.Application.Billing;
using OrcaFacil.Application.Jobs;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Billing;

public sealed class BillingBackgroundWorker(
    IServiceScopeFactory scopes,
    IOptions<BillingOptions> options,
    IDatabaseConfigurationState databaseConfiguration,
    ILogger<BillingBackgroundWorker> logger) : BackgroundService
{
    private readonly string _instance = $"{Environment.MachineName}:{Guid.NewGuid():N}";
    private readonly BillingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BILLING_BACKGROUND_WORKER_STARTED Instance {Instance}", _instance);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _options.SyncIntervalMinutes));

            if (!databaseConfiguration.IsValid)
            {
                logger.LogWarning("BILLING_BACKGROUND_WORKER_SKIPPED_DATABASE_NOT_CONFIGURED");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                continue;
            }

            try
            {
                using var scope = scopes.CreateScope();
                var lockService = scope.ServiceProvider.GetRequiredService<IJobLockService>();
                var lease = TimeSpan.FromMinutes(10);

                var acquired = await lockService.TryAcquireAsync("billing_status_sync", _instance, lease, stoppingToken);
                if (acquired)
                {
                    try
                    {
                        var billingService = scope.ServiceProvider.GetRequiredService<BillingStatusService>();
                        var processed = await billingService.SyncOverdueSubscriptionsAsync(stoppingToken);
                        if (processed > 0)
                        {
                            logger.LogInformation("BILLING_STATUS_SYNC_COMPLETED SubscriptionsProcessed {Count}", processed);
                        }
                    }
                    finally
                    {
                        await lockService.ReleaseAsync("billing_status_sync", _instance, stoppingToken);
                    }
                }
                else
                {
                    logger.LogDebug("BILLING_STATUS_SYNC_SKIPPED_LOCK_NOT_ACQUIRED");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BILLING_BACKGROUND_WORKER_ERROR");
            }

            await Task.Delay(interval, stoppingToken);
        }

        logger.LogInformation("BILLING_BACKGROUND_WORKER_STOPPED Instance {Instance}", _instance);
    }
}
