using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.GoLive;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services.GoLive;

public sealed class GoLivePersistenceService(OrcaFacilDbContext db, GoLiveChecklistService rules)
{
    public async Task<IReadOnlyList<GoLiveChecklistItem>> GetOrCreateAsync(Guid accountId, CancellationToken ct)
    {
        var items = await db.GoLiveChecklistItems.Where(x => x.AccountId == accountId).OrderBy(x => x.Title).ToListAsync(ct);
        if (items.Count > 0) return items;
        items = GoLiveChecklistCatalog.Items.Select(x => new GoLiveChecklistItem { AccountId=accountId, Code=x.Code, Title=x.Title, IsCritical=x.Critical, IsAutomatic=x.Automatic }).ToList();
        db.GoLiveChecklistItems.AddRange(items); await db.SaveChangesAsync(ct); return items;
    }

    public async Task CompleteManualAsync(Guid accountId, Guid itemId, Guid userId, string responsible, string observation, bool confirmed, CancellationToken ct)
    {
        var item = await db.GoLiveChecklistItems.SingleOrDefaultAsync(x=>x.Id==itemId && x.AccountId==accountId, ct) ?? throw new KeyNotFoundException("Item não encontrado nesta conta.");
        rules.CompleteManual(item, accountId, userId, responsible, observation, confirmed);
        db.AuditLogs.Add(new AuditLog { AccountId=accountId, UserId=userId, Action="GoLive.ManualItemCompleted", EntityType=nameof(GoLiveChecklistItem), EntityId=item.Id.ToString(), Summary=$"Item {item.Code} confirmado.", CorrelationId=Guid.NewGuid() });
        await db.SaveChangesAsync(ct);
    }
}
