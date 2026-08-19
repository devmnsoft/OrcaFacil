using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.WorkOrders;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<WorkOrder> Orders { get; private set; } = [];
    public string? Search { get; private set; }
    public WorkOrderStatus? Status { get; private set; }
    public string? Origin { get; private set; }
    public async Task OnGetAsync(string? search, WorkOrderStatus? status, string? origin, DateTime? from, DateTime? to, CancellationToken ct)
    {
        Search = search; Status = status; Origin = origin; var accountId = account.AccountId; if (accountId is null) return;
        var query = db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Number.Contains(search) || x.Title.Contains(search));
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (origin == "proposal") query = query.Where(x => x.SourceDocumentId != null);
        if (origin == "manual") query = query.Where(x => x.SourceDocumentId == null);
        if (from.HasValue) query = query.Where(x => x.ScheduledStart >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(x => x.ScheduledStart < to.Value.Date.AddDays(1).ToUniversalTime());
        Orders = await query.OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
    }
}
