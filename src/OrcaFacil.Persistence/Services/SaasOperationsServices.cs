using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Jobs;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services;

public sealed class JobLockService(OrcaFacilDbContext db) : IJobLockService
{
    public async Task<bool> TryAcquireAsync(string name, string instanceId, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var updated = await db.JobLocks.Where(x => x.Name == name && (x.ReleasedAt != null || x.LockedUntil <= now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LockedBy, instanceId).SetProperty(x => x.LockedUntil, now.Add(lease))
                .SetProperty(x => x.AcquiredAt, now).SetProperty(x => x.ReleasedAt, (DateTime?)null)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        if (updated == 1) return true;
        if (await db.JobLocks.AsNoTracking().AnyAsync(x => x.Name == name, cancellationToken)) return false;
        db.JobLocks.Add(new JobLock(name));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await db.JobLocks.AsNoTracking().AnyAsync(x => x.Name == name, cancellationToken)) return false;
            throw;
        }
        return await TryAcquireAsync(name, instanceId, lease, cancellationToken);
    }

    public async Task ReleaseAsync(string name, string instanceId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await db.JobLocks.Where(x => x.Name == name && x.LockedBy == instanceId && x.ReleasedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReleasedAt, now).SetProperty(x => x.LockedUntil, now).SetProperty(x => x.UpdatedAt, now), cancellationToken);
    }
}

public sealed class ProcessingOutboxService(OrcaFacilDbContext db) : IProcessingOutboxService
{
    public async Task<ProcessingOutboxItem> EnqueueAsync(Guid accountId, string type, string idempotencyKey, string payloadJson, int priority = 0, int maximumAttempts = 5, CancellationToken cancellationToken = default)
    {
        var existing = await db.ProcessingOutbox.SingleOrDefaultAsync(x => x.AccountId == accountId && x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null) return existing;
        var item = new ProcessingOutboxItem(accountId, type, idempotencyKey, payloadJson, priority, maximumAttempts);
        db.ProcessingOutbox.Add(item); await db.SaveChangesAsync(cancellationToken); return item;
    }

    public async Task<IReadOnlyList<ProcessingOutboxItem>> ClaimAsync(string instanceId, int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var candidateIds = await db.ProcessingOutbox.AsNoTracking().Where(x => !x.IsDeleted && x.Status == OutboxStatus.Pending && x.NextAttemptAt <= now && x.Attempts < x.MaximumAttempts)
            .OrderByDescending(x => x.Priority).ThenBy(x => x.CreatedAt).Select(x => x.Id).Take(Math.Clamp(batchSize, 1, 100)).ToListAsync(cancellationToken);
        var claimedIds = new List<Guid>();
        foreach (var id in candidateIds)
        {
            var won = await db.ProcessingOutbox.Where(x => x.Id == id && x.Status == OutboxStatus.Pending && x.NextAttemptAt <= now && x.Attempts < x.MaximumAttempts)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, OutboxStatus.Processing)
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1).SetProperty(x => x.ProcessingStartedAt, now)
                    .SetProperty(x => x.ProcessingInstanceId, instanceId).SetProperty(x => x.UpdatedAt, now), cancellationToken);
            if (won == 1) claimedIds.Add(id);
        }
        return await db.ProcessingOutbox.AsNoTracking().Where(x => claimedIds.Contains(x.Id)).ToListAsync(cancellationToken);
    }

    public async Task CompleteAsync(Guid id, CancellationToken cancellationToken = default) { var item = await db.ProcessingOutbox.SingleAsync(x => x.Id == id, cancellationToken); item.Complete(); await db.SaveChangesAsync(cancellationToken); }
    public async Task FailAsync(Guid id, string safeError, CancellationToken cancellationToken = default) { var item = await db.ProcessingOutbox.SingleAsync(x => x.Id == id, cancellationToken); item.Fail(safeError, DateTime.UtcNow.AddMinutes(Math.Pow(2, item.Attempts))); await db.SaveChangesAsync(cancellationToken); }
    public async Task<bool> RequeueFailedAsync(Guid id, CancellationToken cancellationToken = default) { var item = await db.ProcessingOutbox.SingleOrDefaultAsync(x => x.Id == id, cancellationToken); if (item is null || !item.RequeueFailed(DateTime.UtcNow)) return false; await db.SaveChangesAsync(cancellationToken); return true; }
}
