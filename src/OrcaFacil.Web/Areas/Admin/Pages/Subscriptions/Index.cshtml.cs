using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Areas.Admin.Pages.Subscriptions;

[Authorize(Policy = "PlatformFinanceOrHigher")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public IReadOnlyList<Row> Items { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) => Items = await db.Subscriptions.AsNoTracking().Where(x => !x.IsDeleted && x.AccountId != null).OrderByDescending(x => x.CreatedAt).Take(200).Select(x => new Row(x.AccountId!.Value, db.BusinessAccounts.Where(a => a.Id == x.AccountId).Select(a => a.DisplayName).FirstOrDefault() ?? "Conta indisponível", x.Plan.ToString(), x.Status.ToString(), x.TrialEndsAt, x.NextDueAt, x.Amount)).ToListAsync(ct);
    public sealed record Row(Guid AccountId, string Account, string Plan, string Status, DateTime? TrialEndsAt, DateTime? NextDueAt, decimal Amount);
}
