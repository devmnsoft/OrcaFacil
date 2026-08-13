using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Reports;
[Authorize]
public sealed class RecurringModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
 public int Active {get;private set;} public int Canceled {get;private set;} public int Expired {get;private set;} public decimal Forecast {get;private set;} public decimal Received {get;private set;} public decimal Overdue {get;private set;} public int RecurringClients {get;private set;} public bool HasStatisticalBase=>Active+Canceled+Expired>=3;
 public async Task OnGetAsync(CancellationToken ct){var contracts=await db.RecurringContracts.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted).ToListAsync(ct);var payments=await db.ContractPayments.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted).ToListAsync(ct);Active=contracts.Count(x=>x.Status==ContractStatus.Active);Canceled=contracts.Count(x=>x.Status==ContractStatus.Canceled);Expired=contracts.Count(x=>x.Status==ContractStatus.Expired);RecurringClients=contracts.Where(x=>x.Status==ContractStatus.Active).Select(x=>x.ClientId).Distinct().Count();Forecast=payments.Where(x=>x.Status is RecurringPaymentStatus.Forecast or RecurringPaymentStatus.Pending or RecurringPaymentStatus.Overdue).Sum(x=>x.Amount);Received=payments.Where(x=>x.Status==RecurringPaymentStatus.Paid).Sum(x=>x.Amount);Overdue=payments.Where(x=>x.Status==RecurringPaymentStatus.Overdue||(x.Status==RecurringPaymentStatus.Pending&&x.DueDate<DateOnly.FromDateTime(DateTime.Today))).Sum(x=>x.Amount);}
}
