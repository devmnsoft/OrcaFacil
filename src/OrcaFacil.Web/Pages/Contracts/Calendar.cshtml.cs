using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Contracts;
[Authorize]
public sealed class CalendarModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
 public DateOnly Start { get; private set; } public DateOnly End { get; private set; } public IReadOnlyList<Event> Events { get; private set; }=[];
 public async Task OnGetAsync(DateOnly? start, DateOnly? end, CancellationToken ct){Start=start??new DateOnly(DateTime.Today.Year,DateTime.Today.Month,1);End=end??Start.AddMonths(1).AddDays(-1);var contracts=await db.RecurringContracts.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted).ToListAsync(ct);var payments=await db.ContractPayments.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted&&x.DueDate>=Start&&x.DueDate<=End).ToListAsync(ct);var events=new List<Event>();foreach(var c in contracts){if(c.EndDate is {} d&&d>=Start&&d<=End)events.Add(new(d,"Renovação",c.Title,c.Id));if(c.NextServiceDate is {} s&&s>=Start&&s<=End)events.Add(new(s,"OS prevista",c.Title,c.Id));}events.AddRange(payments.Select(p=>new Event(p.DueDate,"Pagamento",p.Amount.ToString("C"),p.ContractId)));Events=events.OrderBy(x=>x.Date).ToList();}
 public sealed record Event(DateOnly Date,string Type,string Title,Guid ContractId);
}
