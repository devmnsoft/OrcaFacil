using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Productivity;

[Authorize(Policy = "Permission:" + PermissionCodes.ProductivityView)]
public sealed class IndexModel(ICurrentAccountService current, OrcaFacilDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    public IReadOnlyDictionary<string, int> Metrics { get; private set; } = new Dictionary<string, int>();
    public async Task OnGetAsync(CancellationToken ct)
    {
        var accountId = current.AccountId ?? throw new UnauthorizedAccessException("Selecione uma conta.");
        var from = DateTime.SpecifyKind(From?.Date ?? DateTime.UtcNow.Date.AddDays(-30), DateTimeKind.Utc);
        var to = DateTime.SpecifyKind((To?.Date ?? DateTime.UtcNow.Date).AddDays(1), DateTimeKind.Utc);
        if (from >= to) throw new ArgumentException("Período inválido.");
        var events = await db.ProductivityEvents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.OccurredAt >= from && x.OccurredAt < to).GroupBy(x => x.EventType).Select(x => new { x.Key, Count = x.Count() }).ToListAsync(ct);
        Metrics = events.ToDictionary(x => x.Key, x => x.Count);
    }
}
