using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Templates;

[Authorize]
public class IndexModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    public IndexModel(OrcaFacilDbContext db) => _db = db;
    public IReadOnlyList<BudgetTemplate> Templates { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) => Templates = await _db.BudgetTemplates.Include(x => x.Items).Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Profession).ToListAsync(ct);
}
