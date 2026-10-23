using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Contracts;
using OrcaFacil.Application.Jobs;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Services;

/// <summary>Generates the next competence of each active recurring contract, marks overdue charges and expires
/// contracts whose term ended. Runs hourly under a job lock; the business date is the account's civil date.</summary>
public sealed class RecurringContractBillingWorker(
    IServiceScopeFactory scopes,
    IDatabaseConfigurationState databaseConfiguration,
    ILogger<RecurringContractBillingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private const string LockName = "recurring_contract_billing";
    private readonly string _instance = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RECURRING_CONTRACT_BILLING_WORKER_STARTED Instance {Instance}", _instance);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!databaseConfiguration.IsValid)
            {
                logger.LogWarning("RECURRING_CONTRACT_BILLING_WORKER_SKIPPED_DATABASE_NOT_CONFIGURED");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                continue;
            }

            try
            {
                using var scope = scopes.CreateScope();
                var lockService = scope.ServiceProvider.GetRequiredService<IJobLockService>();

                if (await lockService.TryAcquireAsync(LockName, _instance, TimeSpan.FromMinutes(20), stoppingToken))
                {
                    try
                    {
                        var billing = scope.ServiceProvider.GetRequiredService<IRecurringContractBillingService>();
                        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CommercialClock.BusinessTimeZone));
                        var result = await billing.RunAsync(today, stoppingToken);
                        if (result.Generated + result.Overdue + result.Expired > 0)
                            logger.LogInformation("RECURRING_CONTRACT_BILLING_COMPLETED Generated {Generated} Overdue {Overdue} Expired {Expired}",
                                result.Generated, result.Overdue, result.Expired);
                    }
                    finally
                    {
                        await lockService.ReleaseAsync(LockName, _instance, stoppingToken);
                    }
                }
                else
                {
                    logger.LogDebug("RECURRING_CONTRACT_BILLING_SKIPPED_LOCK_NOT_ACQUIRED");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RECURRING_CONTRACT_BILLING_WORKER_ERROR");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("RECURRING_CONTRACT_BILLING_WORKER_STOPPED Instance {Instance}", _instance);
    }
}
