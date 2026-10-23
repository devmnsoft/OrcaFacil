using OrcaFacil.Application.Jobs;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Services;

/// <summary>Marks scheduled work orders as Overdue once their planned end passed.
/// Overdue is unreachable from any user action, so without this worker the dashboard KPI stayed at zero.</summary>
public sealed class WorkOrderOverdueWorker(
    IServiceScopeFactory scopes,
    IDatabaseConfigurationState databaseConfiguration,
    ILogger<WorkOrderOverdueWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private readonly string _instance = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WORK_ORDER_OVERDUE_WORKER_STARTED Instance {Instance}", _instance);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!databaseConfiguration.IsValid)
            {
                logger.LogWarning("WORK_ORDER_OVERDUE_WORKER_SKIPPED_DATABASE_NOT_CONFIGURED");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                continue;
            }

            try
            {
                using var scope = scopes.CreateScope();
                var lockService = scope.ServiceProvider.GetRequiredService<IJobLockService>();
                var lease = TimeSpan.FromMinutes(10);

                if (await lockService.TryAcquireAsync("work_order_overdue", _instance, lease, stoppingToken))
                {
                    try
                    {
                        var overdue = scope.ServiceProvider.GetRequiredService<IWorkOrderOverdueService>();
                        var marked = await overdue.MarkOverdueAsync(stoppingToken);
                        if (marked > 0) logger.LogInformation("WORK_ORDER_OVERDUE_MARKED Count {Count}", marked);
                    }
                    finally
                    {
                        await lockService.ReleaseAsync("work_order_overdue", _instance, stoppingToken);
                    }
                }
                else
                {
                    logger.LogDebug("WORK_ORDER_OVERDUE_WORKER_SKIPPED_LOCK_NOT_ACQUIRED");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WORK_ORDER_OVERDUE_WORKER_ERROR");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("WORK_ORDER_OVERDUE_WORKER_STOPPED Instance {Instance}", _instance);
    }
}
