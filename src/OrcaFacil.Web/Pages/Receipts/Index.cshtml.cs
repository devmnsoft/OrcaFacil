using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.Receipts;
[Authorize] public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<Receipt> Receipts { get; private set; }=[];
    public async Task OnGetAsync(string? method, DateTime? from, DateTime? to, CancellationToken ct){var q=db.Receipts.AsNoTracking().Where(x=>x.AccountId==account.AccountId&&!x.IsDeleted);if(!string.IsNullOrWhiteSpace(method))q=q.Where(x=>x.PaymentMethod==method);if(from.HasValue)q=q.Where(x=>x.IssuedAt>=from);if(to.HasValue)q=q.Where(x=>x.IssuedAt<to.Value.AddDays(1));Receipts=await q.OrderByDescending(x=>x.IssuedAt).Take(200).ToListAsync(ct);}
}
