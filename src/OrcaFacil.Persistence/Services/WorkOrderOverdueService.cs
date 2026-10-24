using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

/// <summary>Cross-account maintenance that turns planned-but-not-started orders into Overdue.
/// Runs in the background: no user action can reach Overdue, so the dashboard KPI was always zero.</summary>
public sealed class WorkOrderOverdueService(OrcaFacilDbContext db) : IWorkOrderOverdueService
{
    public async Task<int> MarkOverdueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var due = await db.WorkOrders
            .Where(x => !x.IsDeleted && x.Status == WorkOrderStatus.Scheduled
                && x.ScheduledEnd != null && x.ScheduledEnd < now)
            .OrderBy(x => x.ScheduledEnd)
            .Take(500)
            .ToListAsync(ct);
        if (due.Count == 0) return 0;

        foreach (var order in due)
        {
            order.Status = WorkOrderStatus.Overdue;
            order.OverdueMarkedAt = now;
            order.Touch();
            db.ActivityEvents.Add(new Domain.Entities.ActivityEvent
            {
                AccountId = order.AccountId,
                ActorUserId = null,
                Action = "WorkOrderOverdue",
                EntityType = "CommercialJourney",
                EntityId = order.Id,
                Summary = $"Ordem vencida sem início até {order.ScheduledEnd!.Value.ToLocalTime():dd/MM/yyyy HH:mm}."
            });
        }

        await db.SaveChangesAsync(ct);
        return due.Count;
    }
}
