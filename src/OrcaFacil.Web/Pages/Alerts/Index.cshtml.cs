using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Alerts;
[Authorize] public sealed class IndexModel(IOperationalAlertService alerts, ICurrentAccountService account, OrcaFacilDbContext db) : PageModel
{
 public IReadOnlyList<Notification> Items { get; private set; } = [];
 public async Task OnGetAsync(CancellationToken ct) { await alerts.GenerateAsync(ct); Items = await Query().OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedAt).ToListAsync(ct); }
 public async Task<IActionResult> OnPostResolveAsync(Guid id, CancellationToken ct) => await Close(id, "Alerta resolvido.", ct);
 public async Task<IActionResult> OnPostIgnoreAsync(Guid id, CancellationToken ct) => await Close(id, "Alerta ignorado.", ct);
 private IQueryable<Notification> Query() { var accountId = account.AccountId ?? Guid.Empty; return db.Notifications.Where(x => x.AccountId == accountId && !x.IsDeleted && x.Message.Contains("[alert:")); }
 private async Task<IActionResult> Close(Guid id, string message, CancellationToken ct) { var item = await Query().SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound(); item.MarkAsRead(); await db.SaveChangesAsync(ct); TempData["Success"] = message; return RedirectToPage(); }
}
