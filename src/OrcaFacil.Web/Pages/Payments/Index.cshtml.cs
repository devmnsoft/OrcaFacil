using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.Payments;
[Authorize] public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account):PageModel
{
 public IReadOnlyList<ManualPayment> Payments{get;private set;}=[];
 public decimal TotalReceived=>Payments.Where(x=>x.Status==OrcaFacil.Domain.Enums.FinancialRecordStatus.Active).Sum(x=>x.Amount);
 public int ActivePayments=>Payments.Count(x=>x.Status==OrcaFacil.Domain.Enums.FinancialRecordStatus.Active);
 public int ReversedPayments=>Payments.Count(x=>x.Status==OrcaFacil.Domain.Enums.FinancialRecordStatus.Reversed);
 public async Task OnGetAsync(CancellationToken ct)=>Payments=await db.ManualPayments.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted).OrderByDescending(x=>x.PaidAt).Take(200).ToListAsync(ct);
}
