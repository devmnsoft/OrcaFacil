using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Contracts;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Contracts;

[Authorize]
public sealed class DashboardModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public int Active { get; private set; } public int Expiring { get; private set; } public int Expired { get; private set; }
    public int Suspended { get; private set; } public int SlaBreaches { get; private set; } public int UsageExceeded { get; private set; }
    public decimal MonthlyRevenue { get; private set; } public IReadOnlyList<Row> AtRisk { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!await account.HasPermissionAsync(PermissionCodes.ContractsAdvancedView, ct)) return Forbid();
        var accountId = account.AccountId!.Value; var today = DateOnly.FromDateTime(DateTime.UtcNow); var limit = today.AddDays(90);
        var contracts = await db.RecurringContracts.AsNoTracking().Where(x=>x.AccountId==accountId&&!x.IsDeleted).ToListAsync(ct);
        Active=contracts.Count(x=>x.Status==ContractStatus.Active); Expiring=contracts.Count(x=>x.EndDate>=today&&x.EndDate<=limit&&x.Status==ContractStatus.Active);
        Expired=contracts.Count(x=>x.Status==ContractStatus.Expired||(x.EndDate<today&&x.Status!=ContractStatus.Canceled&&x.Status!=ContractStatus.Terminated)); Suspended=contracts.Count(x=>x.Status==ContractStatus.Suspended);
        SlaBreaches=await db.ServiceLevelBreaches.CountAsync(x=>x.AccountId==accountId&&!x.IsDeleted&&x.ResolvedAt==null,ct);
        UsageExceeded=await db.ContractUsageAllowances.CountAsync(x=>x.AccountId==accountId&&!x.IsDeleted&&x.UsedQuantity>x.AllowanceQuantity,ct);
        if(await account.HasPermissionAsync(PermissionCodes.FinanceView,ct)) MonthlyRevenue=contracts.Where(x=>x.Status==ContractStatus.Active).Sum(x=>Monthly(x.RecurringAmount,x.Periodicity));
        var latest=await db.ContractHealthSnapshots.AsNoTracking().Where(x=>x.AccountId==accountId&&!x.IsDeleted&&x.Score<60).OrderByDescending(x=>x.CalculatedAt).ToListAsync(ct);
        AtRisk=latest.GroupBy(x=>x.ContractId).Select(x=>x.First()).Take(20).Join(contracts,x=>x.ContractId,c=>c.Id,(x,c)=>new Row(c.Id,c.Number,c.Title,x.Score,x.Classification,x.NextAction)).ToList(); return Page();
    }
    private static decimal Monthly(decimal amount,RecurrencePeriod p)=>p switch {RecurrencePeriod.Bimonthly=>amount/2,RecurrencePeriod.Quarterly=>amount/3,RecurrencePeriod.Semiannual=>amount/6,RecurrencePeriod.Annual=>amount/12,_=>amount};
    public sealed record Row(Guid Id,string Number,string Title,int Score,string Classification,string NextAction);
}
