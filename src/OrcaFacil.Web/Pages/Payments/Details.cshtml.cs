using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.Payments;
[Authorize] public sealed class DetailsModel(OrcaFacilDbContext db,ICurrentAccountService account,IReceiptApplicationService receipts):PageModel
{
 public ManualPayment? Payment{get;private set;} public Receipt? Receipt{get;private set;} [BindProperty] public string ReversalReason{get;set;}=string.Empty;
 public async Task<IActionResult> OnGetAsync(Guid id,CancellationToken ct){Payment=await db.ManualPayments.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==account.AccountId&&!x.IsDeleted,ct);if(Payment is null)return NotFound();Receipt=await db.Receipts.AsNoTracking().SingleOrDefaultAsync(x=>x.PaymentId==id&&x.AccountId==account.AccountId&&!x.IsDeleted,ct);return Page();}
 public async Task<IActionResult> OnPostReverseAsync(Guid id,CancellationToken ct){if(string.IsNullOrWhiteSpace(ReversalReason)){TempData["Error"]="Informe o motivo do estorno interno.";return RedirectToPage(new{id});}TempData[await receipts.ReversePaymentAsync(id,ReversalReason,ct)?"Success":"Error"]="Correção interna processada.";return RedirectToPage(new{id});}
}
