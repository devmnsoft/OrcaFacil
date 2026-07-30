using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Areas.Admin.Pages.Accounts;
[Authorize(Policy="SuperAdminOnly")]
public sealed class DetailsModel(OrcaFacilDbContext db):PageModel
{
 public Account360ViewModel Account { get;private set; }=default!;
 public async Task<IActionResult> OnGetAsync(Guid id,CancellationToken ct)
 {
  var a=await db.BusinessAccounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&!x.IsDeleted,ct); if(a is null)return NotFound();
  var members=await db.AccountMembers.AsNoTracking().CountAsync(x=>x.AccountId==id&&!x.IsDeleted,ct);
  var subscription=await db.Subscriptions.AsNoTracking().OrderByDescending(x=>x.CreatedAt).FirstOrDefaultAsync(x=>x.AccountId==id&&!x.IsDeleted,ct);
  Account=new(a.Id,a.DisplayName,Mask(a.DocumentNumber),a.Email,a.CreatedAt,a.Status.ToString(),subscription?.Plan.ToString()??a.CurrentPlanCode,subscription?.Status.ToString()??"Grátis",subscription?.NextDueAt,members);
  return Page();
 }
 private static string Mask(string? value)=>string.IsNullOrWhiteSpace(value)?"Não informado":value.Length<5?"***":$"***{value[^4..]}";
}
public sealed record Account360ViewModel(Guid Id,string Name,string MaskedDocument,string Email,DateTime CreatedAt,string Status,string SelectedPlan,string PaymentStatus,DateTime? DueAt,int Members);
