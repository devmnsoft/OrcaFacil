using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Areas.Admin.Pages.Onboarding;
[Authorize(Policy="SuperAdminOnly")]
public sealed class IndexModel(OrcaFacilDbContext db):PageModel
{
 public int TotalAccounts{get;private set;} public int Incomplete{get;private set;} public int FirstBudgets{get;private set;} public int Abandoned{get;private set;} public IReadOnlyList<Row> Rows{get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct){var cutoff=DateTime.UtcNow.AddDays(-7);TotalAccounts=await db.BusinessAccounts.CountAsync(x=>!x.IsDeleted,ct);var query=db.AccountOnboardingStates.AsNoTracking().Where(x=>!x.IsDeleted);Incomplete=await query.CountAsync(x=>x.CompletedAt==null,ct);FirstBudgets=await query.CountAsync(x=>x.FirstBudgetCompletedAt!=null||x.FirstBudgetStartedAt!=null,ct);Abandoned=await query.CountAsync(x=>x.CompletedAt==null&&x.LastSeenAt<cutoff,ct);Rows=await query.OrderByDescending(x=>x.LastSeenAt).Take(100).Select(x=>new Row(x.AccountId.ToString().Substring(0,8),x.CurrentStep.ToString(),x.CompletedAt!=null,x.FirstBudgetStartedAt!=null,x.LastSeenAt,x.CompletedAt==null&&x.LastSeenAt<cutoff)).ToListAsync(ct);}
 public sealed record Row(string AccountReference,string Step,bool Completed,bool HasBudget,DateTime LastSeen,bool Abandoned);
}
