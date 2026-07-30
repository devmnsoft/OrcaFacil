using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Schedule;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<WorkOrder> Orders { get; private set; } = [];
    public async Task OnGetAsync(string view = "week", CancellationToken ct = default)
    {
        var start = DateTime.UtcNow.Date; var end = view == "today" ? start.AddDays(1) : start.AddDays(7);
        Orders = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == account.AccountId && !x.IsDeleted && x.ScheduledStart >= start && x.ScheduledStart < end).OrderBy(x => x.ScheduledStart).Take(100).ToListAsync(ct);
    }
}
