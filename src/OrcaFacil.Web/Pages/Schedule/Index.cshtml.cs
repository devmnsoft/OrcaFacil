using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Schedule;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<WorkOrder> Orders { get; private set; } = [];
    public string View { get; private set; } = "week";
    public async Task OnGetAsync(string view = "week", WorkOrderStatus? status = null, CancellationToken ct = default)
    {
        View = view; var start = DateTime.UtcNow.Date; var end = view == "today" ? start.AddDays(1) : start.AddDays(7);
        var query = db.WorkOrders.AsNoTracking().Where(x => x.AccountId == account.AccountId && !x.IsDeleted && x.Status != WorkOrderStatus.Completed && x.Status != WorkOrderStatus.Cancelled);
        query = view switch { "overdue" => query.Where(x => x.ScheduledEnd < DateTime.UtcNow), "unscheduled" => query.Where(x => x.ScheduledStart == null), _ => query.Where(x => x.ScheduledStart >= start && x.ScheduledStart < end) };
        if (status.HasValue) query = query.Where(x => x.Status == status);
        Orders = await query.OrderBy(x => x.ScheduledStart).Take(100).ToListAsync(ct);
    }
}
