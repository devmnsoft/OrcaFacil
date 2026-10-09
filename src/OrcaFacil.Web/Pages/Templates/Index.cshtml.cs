using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Templates;

[Authorize]
public class IndexModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    private readonly ICurrentAccountService _currentAccount;

    public IndexModel(OrcaFacilDbContext db, ICurrentAccountService currentAccount)
    {
        _db = db;
        _currentAccount = currentAccount;
    }

    public IReadOnlyList<BudgetTemplate> AccountTemplates { get; private set; } = [];
    public IReadOnlyList<BudgetTemplate> SystemTemplates { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        AccountTemplates = await _db.BudgetTemplates
            .Include(x => x.Items)
            .Where(x => !x.IsDeleted && !x.IsSystemTemplate && (x.AccountId == accountId || (x.AccountId == null && x.UserId == userId)))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        SystemTemplates = await _db.BudgetTemplates
            .Include(x => x.Items)
            .Where(x => x.IsActive && !x.IsDeleted && x.IsSystemTemplate)
            .OrderBy(x => x.Profession)
            .ThenBy(x => x.Title)
            .ToListAsync(ct);

        return Page();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id, CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        var template = await _db.BudgetTemplates.SingleOrDefaultAsync(
            x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate && (x.AccountId == accountId || (x.AccountId == null && x.UserId == userId)), ct);
        
        if (template is null) return NotFound();

        template.IsActive = !template.IsActive;
        template.Touch();
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = template.IsActive ? "Modelo ativado com sucesso." : "Modelo inativado com sucesso.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        var template = await _db.BudgetTemplates.SingleOrDefaultAsync(
            x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate && (x.AccountId == accountId || (x.AccountId == null && x.UserId == userId)), ct);

        if (template is null) return NotFound();

        template.MarkAsDeleted();
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Modelo removido com sucesso.";
        return RedirectToPage();
    }
}
