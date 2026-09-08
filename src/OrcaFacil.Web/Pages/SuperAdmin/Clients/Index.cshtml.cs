using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc.RazorPages; using Microsoft.EntityFrameworkCore; using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.SuperAdmin.Clients;
[Authorize(Policy="SuperAdminOnly")]
public sealed class IndexModel(OrcaFacilDbContext db):PageModel
{
 public string? Query{get;private set;} public string? Status{get;private set;} public string[] Statuses{get;}=["Active","Blocked","Inactive","Closed"]; public IReadOnlyList<Row> Accounts{get;private set;}=[];
 public async Task OnGetAsync(string? q,string? status,CancellationToken ct){Query=q?.Trim();Status=status;var query=db.BusinessAccounts.AsNoTracking().Where(x=>!x.IsDeleted);if(!string.IsNullOrWhiteSpace(Query))query=query.Where(x=>EF.Functions.ILike(x.DisplayName,$"%{Query}%")||EF.Functions.ILike(x.Email,$"%{Query}%")||(x.DocumentNumber!=null&&x.DocumentNumber.Contains(Query)));if(Enum.TryParse<OrcaFacil.Domain.Enums.AccountStatus>(status,true,out var parsed))query=query.Where(x=>x.Status==parsed);Accounts=await query.OrderBy(x=>x.DisplayName).Select(x=>new Row(x.Id,x.DisplayName,x.DocumentNumber??"—",x.Email,x.Status.ToString(),x.CurrentPlanCode,db.AccountModuleSubscriptions.Count(s=>s.AccountId==x.Id&&!s.IsDeleted&&(s.Status==OrcaFacil.Domain.Entities.SaasModuleSubscriptionStatus.Active||s.Status==OrcaFacil.Domain.Entities.SaasModuleSubscriptionStatus.Trial)),db.AccountMembers.Count(m=>m.AccountId==x.Id&&!m.IsDeleted))).ToListAsync(ct);}
 public sealed record Row(Guid Id,string Name,string Document,string Email,string Status,string Plan,int Modules,int Users);
}
