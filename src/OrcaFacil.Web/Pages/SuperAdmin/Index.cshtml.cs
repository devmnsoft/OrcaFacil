using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.SuperAdmin;

[Authorize(Policy="SuperAdminOnly")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    public GlobalMetrics Metrics { get; private set; } = new();
    public IReadOnlyList<ModuleMetric> TopModules { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        Metrics = new GlobalMetrics
        {
            ActiveAccounts = await db.BusinessAccounts.CountAsync(x=>!x.IsDeleted && x.Status==AccountStatus.Active,ct),
            BlockedAccounts = await db.BusinessAccounts.CountAsync(x=>!x.IsDeleted && x.Status==AccountStatus.Blocked,ct),
            OverdueAccounts = await db.BusinessAccounts.CountAsync(x=>!x.IsDeleted && x.FinancialStatus=="Overdue",ct),
            ActiveUsers = await db.Users.CountAsync(x=>!x.IsDeleted && x.IsActive && !x.IsBlocked,ct),
            TrialSubscriptions = await db.AccountModuleSubscriptions.CountAsync(x=>!x.IsDeleted && x.Status==SaasModuleSubscriptionStatus.Trial && x.TrialEndsAt>DateTime.UtcNow,ct),
            MonthlyRecurringRevenue = await db.AccountModuleSubscriptions.Where(x=>!x.IsDeleted && x.Status==SaasModuleSubscriptionStatus.Active).SumAsync(x=>(decimal?)(x.BillingPeriod==SaasBillingPeriod.Annual ? x.ContractedPrice/12m : x.ContractedPrice-x.DiscountAmount),ct) ?? 0
        };
        TopModules = await db.SaasModules.AsNoTracking().Where(x=>x.IsActive&&!x.IsDeleted)
            .Select(module=>new ModuleMetric(module.DisplayName,
                db.AccountModuleSubscriptions.Count(x=>x.ModuleId==module.Id&&!x.IsDeleted&&(x.Status==SaasModuleSubscriptionStatus.Active||x.Status==SaasModuleSubscriptionStatus.Trial)),
                db.AccountModuleUsageEvents.Count(x=>x.ModuleCode==module.Code&&!x.IsDeleted&&x.OccurredAt>=since)))
            .OrderByDescending(x=>x.Subscriptions).ThenBy(x=>x.Name).Take(8).ToListAsync(ct);
    }
    public sealed class GlobalMetrics { public int ActiveAccounts{get;set;} public int BlockedAccounts{get;set;} public int TrialSubscriptions{get;set;} public int OverdueAccounts{get;set;} public int ActiveUsers{get;set;} public decimal MonthlyRecurringRevenue{get;set;} }
    public sealed record ModuleMetric(string Name,int Subscriptions,int Usage);
}
