using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class PreviewModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly ICurrentAccountService _account;
    public PreviewModel(OrcaFacilDbContext db, ICurrentUserService current, ICurrentAccountService account) { _db = db; _current = current; _account = account; }
    public Document Document { get; private set; } = default!;
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var document = await _db.Documents.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.UserId == _current.UserId && x.AccountId == _account.AccountId && !x.IsDeleted, ct);
        if (document is null) return NotFound();
        Document = document;
        return Page();
    }
}
