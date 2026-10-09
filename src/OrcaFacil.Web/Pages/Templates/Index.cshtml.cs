using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Templates;

[Authorize]
public class IndexModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    private readonly ICurrentAccountService _currentAccount;
    private readonly IAuditService _audit;

    public IndexModel(OrcaFacilDbContext db, ICurrentAccountService currentAccount, IAuditService audit)
    {
        _db = db;
        _currentAccount = currentAccount;
        _audit = audit;
    }

    public IReadOnlyList<BudgetTemplate> AccountTemplates { get; private set; } = [];
    public IReadOnlyList<BudgetTemplate> SystemTemplates { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        if (_currentAccount.HasAccount && accountId.HasValue)
        {
            try { await _currentAccount.EnsureAccountAccessAsync(ct); }
            catch (UnauthorizedAccessException) { return Forbid(); }

            AccountTemplates = await _db.BudgetTemplates
                .Include(x => x.Items)
                .Where(x => !x.IsDeleted && !x.IsSystemTemplate && x.AccountId == accountId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }
        else if (userId != Guid.Empty && !accountId.HasValue)
        {
            // Legado pessoal estrito: somente o próprio proprietário
            AccountTemplates = await _db.BudgetTemplates
                .Include(x => x.Items)
                .Where(x => !x.IsDeleted && !x.IsSystemTemplate && x.AccountId == null && x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }
        else
        {
            AccountTemplates = [];
        }

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

        if (_currentAccount.HasAccount && accountId.HasValue)
        {
            try { await _currentAccount.EnsureAccountAccessAsync(ct); }
            catch (UnauthorizedAccessException) { return Forbid(); }

            var canManage = await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentTemplatesManage, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct);
            if (!canManage)
            {
                TempData["Error"] = "Você não possui permissão para alterar modelos.";
                return RedirectToPage();
            }
        }

        var template = await _db.BudgetTemplates.SingleOrDefaultAsync(
            x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate &&
                 (accountId.HasValue ? x.AccountId == accountId.Value : x.AccountId == null && x.UserId == userId), ct);
        
        if (template is null) return NotFound();

        var before = new { template.IsActive };
        template.IsActive = !template.IsActive;
        template.Touch();

        await _audit.RegisterAsync(userId, "ToggleActive", "BudgetTemplate", template.Id.ToString(), before, new { template.IsActive }, null, ct, accountId);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = template.IsActive ? "Modelo ativado com sucesso." : "Modelo inativado com sucesso.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        if (_currentAccount.HasAccount && accountId.HasValue)
        {
            try { await _currentAccount.EnsureAccountAccessAsync(ct); }
            catch (UnauthorizedAccessException) { return Forbid(); }

            var canManage = await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentTemplatesManage, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct);
            if (!canManage)
            {
                TempData["Error"] = "Você não possui permissão para remover modelos.";
                return RedirectToPage();
            }
        }

        var template = await _db.BudgetTemplates.SingleOrDefaultAsync(
            x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate &&
                 (accountId.HasValue ? x.AccountId == accountId.Value : x.AccountId == null && x.UserId == userId), ct);

        if (template is null) return NotFound();

        template.MarkAsDeleted();
        await _audit.RegisterAsync(userId, "Delete", "BudgetTemplate", template.Id.ToString(), null, null, null, ct, accountId);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Modelo removido com sucesso.";
        return RedirectToPage();
    }
}
