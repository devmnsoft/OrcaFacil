using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class DetailsModel(OrcaFacilDbContext db, ICurrentAccountService account, DocumentService documents,
    ICommercialJourneyService journey) : PageModel
{
    public Document? Document { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Document = await db.Documents.Include(x => x.Items).AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
        return Document is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        if (!await OwnsAsync(id, ct)) return NotFound();
        await documents.DeleteAsync(new(account.UserId, id), ct);
        return RedirectToPage("/Documents/Index");
    }

    public async Task<IActionResult> OnPostDuplicateAsync(Guid id, CancellationToken ct)
    {
        if (!await OwnsAsync(id, ct)) return NotFound();
        var result = await documents.DuplicateAsync(new(account.UserId, id), ct);
        return result.Succeeded ? RedirectToPage("/Documents/Edit", new { id = result.Value }) : RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPublicLinkAsync(Guid id, CancellationToken ct)
    {
        if (!await OwnsAsync(id, ct)) return NotFound();
        var result = await journey.CreatePublicAccessAsync(id, TimeSpan.FromDays(30), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    private Task<bool> OwnsAsync(Guid id, CancellationToken ct) => db.Documents.AsNoTracking().AnyAsync(
        x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
}
