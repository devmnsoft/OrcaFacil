using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Templates;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    public DetailsModel(OrcaFacilDbContext db) => _db = db;
    public BudgetTemplate? Template { get; private set; }
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Template = await _db.BudgetTemplates.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.IsActive && !x.IsDeleted, ct);
        return Template is null ? NotFound() : Page();
    }
}
