using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Areas.Admin.Pages;
[Authorize(Policy = "SuperAdminOnly")]
public sealed class ReportsModel(OrcaFacilDbContext db) : PageModel
{
 public int ActiveAccounts { get; private set; } public int InactiveAccounts { get; private set; } public int Trials { get; private set; } public int Paid { get; private set; }
 public int Documents { get; private set; } public int Clients { get; private set; } public int Receipts { get; private set; } public int WorkOrders { get; private set; }
 public IReadOnlyList<PlanUsage> Usage { get; private set; } = [];
 public async Task OnGetAsync(CancellationToken ct) { ActiveAccounts=await db.BusinessAccounts.CountAsync(x=>!x.IsDeleted&&x.Status==AccountStatus.Active,ct); InactiveAccounts=await db.BusinessAccounts.CountAsync(x=>!x.IsDeleted&&x.Status!=AccountStatus.Active,ct); Trials=await db.Subscriptions.CountAsync(x=>!x.IsDeleted&&x.Status==SubscriptionStatus.Trial,ct); Paid=await db.Subscriptions.CountAsync(x=>!x.IsDeleted&&x.Status==SubscriptionStatus.Active,ct); Documents=await db.Documents.CountAsync(x=>!x.IsDeleted,ct); Clients=await db.Clients.CountAsync(x=>!x.IsDeleted,ct); Receipts=await db.Receipts.CountAsync(x=>!x.IsDeleted,ct); WorkOrders=await db.WorkOrders.CountAsync(x=>!x.IsDeleted,ct); Usage=await db.Subscriptions.AsNoTracking().Where(x=>!x.IsDeleted).GroupBy(x=>x.Plan).Select(g=>new PlanUsage(g.Key.ToString(),g.Count())).OrderByDescending(x=>x.Accounts).ToListAsync(ct); }
 public sealed record PlanUsage(string Plan,int Accounts);
}
