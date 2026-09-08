using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Saas.Modules;
using OrcaFacil.Application.Saas.Usage;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services.Saas;

public sealed class ModuleUsageEventService(IModuleUsageTracker tracker) : IModuleUsageEventService
{
    public Task RegisterAsync(Guid accountId, Guid? userId, string moduleCode, string eventCode, string? featureCode,
        string? route, string correlationId, CancellationToken ct = default) =>
        tracker.TrackAsync(accountId, userId, moduleCode, eventCode, featureCode, route, correlationId, ct);
}

public sealed class ModuleUsageSnapshotService(OrcaFacilDbContext db) : IModuleUsageSnapshotService
{
    public async Task RefreshAsync(Guid accountId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
    {
        if (periodEnd <= periodStart) throw new ArgumentException("O fim do período deve ser posterior ao início.");
        var aggregates = await db.AccountModuleUsageEvents.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.OccurredAt >= periodStart && x.OccurredAt < periodEnd && !x.IsDeleted)
            .GroupBy(x => x.ModuleCode)
            .Select(group => new { ModuleCode = group.Key, EventCount = group.LongCount(), ActiveUsers = group.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().Count(), LastUsedAt = group.Max(x => (DateTime?)x.OccurredAt) })
            .ToListAsync(ct);
        foreach (var aggregate in aggregates)
        {
            var snapshot = await db.AccountModuleUsageSnapshots.SingleOrDefaultAsync(x => x.AccountId == accountId && x.ModuleCode == aggregate.ModuleCode && x.PeriodStart == periodStart && !x.IsDeleted, ct);
            if (snapshot is null) db.AccountModuleUsageSnapshots.Add(new AccountModuleUsageSnapshot { AccountId = accountId, ModuleCode = aggregate.ModuleCode, PeriodStart = periodStart, PeriodEnd = periodEnd, EventCount = aggregate.EventCount, ActiveUsers = aggregate.ActiveUsers, LastUsedAt = aggregate.LastUsedAt });
            else { snapshot.PeriodEnd = periodEnd; snapshot.EventCount = aggregate.EventCount; snapshot.ActiveUsers = aggregate.ActiveUsers; snapshot.LastUsedAt = aggregate.LastUsedAt; snapshot.Touch(); }
        }
        await db.SaveChangesAsync(ct);
    }
}
