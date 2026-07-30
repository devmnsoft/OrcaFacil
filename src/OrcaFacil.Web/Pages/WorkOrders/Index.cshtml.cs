using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.WorkOrders;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<WorkOrder> Orders { get; private set; } = [];
    public string? Search { get; private set; }
    public async Task OnGetAsync(string? search, CancellationToken ct)
    {
        Search = search; var accountId = account.AccountId; if (accountId is null) return;
        var query = db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Number.Contains(search) || x.Title.Contains(search));
        Orders = await query.OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
    }
}
