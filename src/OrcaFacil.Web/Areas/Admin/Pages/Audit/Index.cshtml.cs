using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Audit;

[Authorize(Policy = "PlatformAuditRead")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public IReadOnlyList<Row> Items { get; private set; } = [];
    public string? Query { get; private set; }

    public async Task OnGetAsync(string? query, CancellationToken cancellationToken)
    {
        Query = query?.Trim();
        var logs = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Query))
            logs = logs.Where(x => x.Action.Contains(Query) || x.EntityType.Contains(Query) || (x.EntityId != null && x.EntityId.Contains(Query)));
        Items = await logs.OrderByDescending(x => x.CreatedAt).Take(200)
            .Select(x => new Row(x.CreatedAt, x.AccountId, x.UserId, x.Action, x.EntityType, x.EntityId)).ToListAsync(cancellationToken);
    }

    public sealed record Row(DateTime CreatedAt, Guid? AccountId, Guid? UserId, string Action, string EntityType, string? EntityId);
}
