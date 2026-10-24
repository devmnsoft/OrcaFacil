using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Jobs;
using OrcaFacil.Application.Payments;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Services;

/// <summary>Duns open receivables by notifying the end client. Moves no money: cadence and caps are enforced
/// on the receivable, and each e-mail is idempotent per day. Runs hourly under a job lock on the business date.</summary>
public sealed class ReceivableReminderWorker(
    IServiceScopeFactory scopes,
    IDatabaseConfigurationState databaseConfiguration,
    ILogger<ReceivableReminderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private const string LockName = "receivable_reminder";
    private readonly string _instance = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RECEIVABLE_REMINDER_WORKER_STARTED Instance {Instance}", _instance);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!databaseConfiguration.IsValid)
            {
                logger.LogWarning("RECEIVABLE_REMINDER_WORKER_SKIPPED_DATABASE_NOT_CONFIGURED");
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
                        var service = scope.ServiceProvider.GetRequiredService<IReceivableReminderService>();
                        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CommercialClock.BusinessTimeZone));
                        var result = await service.RunAsync(today, stoppingToken);
                        if (result.Reminded > 0)
                            logger.LogInformation("RECEIVABLE_REMINDER_COMPLETED Accounts {Accounts} Reminded {Reminded} Emails {Emails} WhatsApps {WhatsApps}",
                                result.Accounts, result.Reminded, result.Emails, result.WhatsApps);
                    }
                    finally
                    {
                        await lockService.ReleaseAsync(LockName, _instance, stoppingToken);
                    }
                }
                else
                {
                    logger.LogDebug("RECEIVABLE_REMINDER_SKIPPED_LOCK_NOT_ACQUIRED");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RECEIVABLE_REMINDER_WORKER_ERROR");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("RECEIVABLE_REMINDER_WORKER_STOPPED Instance {Instance}", _instance);
    }
}
