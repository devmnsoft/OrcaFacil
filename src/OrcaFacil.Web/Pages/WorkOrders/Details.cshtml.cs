using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.WorkOrders;
[Authorize]
public sealed class DetailsModel(OrcaFacilDbContext db, ICurrentAccountService account, ICommercialJourneyService journey) : PageModel
{
    public WorkOrder? Order { get; private set; }
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) { Order = await Find(id, ct); return Order is null ? NotFound() : Page(); }
    public async Task<IActionResult> OnPostStartAsync(Guid id, CancellationToken ct) { await journey.StartAsync(id, ct); return RedirectToPage(new { id }); }
    public async Task<IActionResult> OnPostCompleteAsync(Guid id, CancellationToken ct) { await journey.CompleteAsync(id, null, ct); return RedirectToPage(new { id }); }
    private Task<WorkOrder?> Find(Guid id, CancellationToken ct) => db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
}
