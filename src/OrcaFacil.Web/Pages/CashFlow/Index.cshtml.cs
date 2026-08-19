using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.CashFlow;
[Authorize] public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
 [BindProperty(SupportsGet=true)] public DateOnly From {get;set;}=new(DateTime.UtcNow.Year,DateTime.UtcNow.Month,1);
 [BindProperty(SupportsGet=true)] public DateOnly To {get;set;}=DateOnly.FromDateTime(DateTime.UtcNow);
 public decimal Received{get;private set;} public decimal Receivable{get;private set;} public decimal Overdue{get;private set;} public decimal Today{get;private set;} public decimal Week{get;private set;} public decimal Month{get;private set;}
 public IReadOnlyList<Group> ByMethod{get;private set;}=[]; public IReadOnlyList<Group> ByOrigin{get;private set;}=[];
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){if(!account.AccountId.HasValue)return Forbid();if(To<From)ModelState.AddModelError(nameof(To),"A data final deve ser posterior à inicial.");
  var start=From.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);var end=To.AddDays(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc);var now=DateTime.UtcNow;var week=now.Date.AddDays(-6);var month=new DateTime(now.Year,now.Month,1,0,0,0,DateTimeKind.Utc);
  var payments=db.ManualPayments.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted&&x.Status==FinancialRecordStatus.Active);
  Received=await payments.Where(x=>x.PaidAt>=start&&x.PaidAt<end).SumAsync(x=>(decimal?)x.Amount,ct)??0;Today=await payments.Where(x=>x.PaidAt>=now.Date).SumAsync(x=>(decimal?)x.Amount,ct)??0;Week=await payments.Where(x=>x.PaidAt>=week).SumAsync(x=>(decimal?)x.Amount,ct)??0;Month=await payments.Where(x=>x.PaidAt>=month).SumAsync(x=>(decimal?)x.Amount,ct)??0;
  ByMethod=await payments.Where(x=>x.PaidAt>=start&&x.PaidAt<end).GroupBy(x=>x.PaymentMethod).Select(x=>new Group(x.Key,x.Sum(v=>v.Amount))).OrderByDescending(x=>x.Total).ToListAsync(ct);
  var entries=db.FinancialEntries.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted&&x.Status!=FinancialEntryStatus.Canceled&&x.DueDate>=From&&x.DueDate<=To);Receivable=await entries.SumAsync(x=>(decimal?)(x.Amount-x.PaidAmount),ct)??0;var today=DateOnly.FromDateTime(now);Overdue=await db.FinancialEntries.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted&&x.Status!=FinancialEntryStatus.Canceled&&x.Status!=FinancialEntryStatus.Paid&&x.DueDate<today).SumAsync(x=>(decimal?)(x.Amount-x.PaidAmount),ct)??0;ByOrigin=await entries.GroupBy(x=>x.Origin).Select(x=>new Group(x.Key.ToString(),x.Sum(v=>v.Amount-v.PaidAmount))).OrderByDescending(x=>x.Total).ToListAsync(ct);return Page();}
 public sealed record Group(string Label,decimal Total);
}
