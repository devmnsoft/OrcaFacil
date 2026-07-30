using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Receipts;
[Authorize]
public sealed class DetailsModel(ICommercialJourneyService journey, OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public Receipt? Receipt { get; private set; } public string Message { get; private set; } = string.Empty;
    public async Task OnGetAsync(Guid paymentId, CancellationToken ct)
    {
        var result = await journey.GenerateReceiptAsync(paymentId, ct); Message = result.Message;
        if (result.EntityId is Guid id) Receipt = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId, ct);
    }
}
