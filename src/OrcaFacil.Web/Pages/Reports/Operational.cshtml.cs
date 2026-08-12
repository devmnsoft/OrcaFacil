using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Reports;

[Authorize]
public sealed class OperationalModel(
    IOperationalAlertService alerts,
    ICurrentAccountService account,
    OrcaFacilDbContext db) : PageModel
{
    public int Open { get; private set; }
    public int Closed { get; private set; }
    public IReadOnlyList<OperationalGroup> Groups { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await alerts.GenerateAsync(ct);
        var accountId = account.AccountId ?? throw new UnauthorizedAccessException("Selecione uma conta para consultar relatórios.");
        var items = await db.Notifications.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.Message.Contains("[alert:"))
            .GroupBy(x => new { x.Category, x.IsRead })
            .Select(x => new { x.Key.Category, x.Key.IsRead, Count = x.Count() })
            .ToListAsync(ct);

        Open = items.Where(x => !x.IsRead).Sum(x => x.Count);
        Closed = items.Where(x => x.IsRead).Sum(x => x.Count);
        Groups = items.GroupBy(x => x.Category.ToString())
            .Select(x => new OperationalGroup(x.Key, x.Where(i => !i.IsRead).Sum(i => i.Count), x.Sum(i => i.Count)))
            .OrderByDescending(x => x.Open)
            .ToArray();
    }

    public sealed record OperationalGroup(string Name, int Open, int Total);
}
