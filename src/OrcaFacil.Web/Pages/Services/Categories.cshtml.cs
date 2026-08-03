using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Services;
[Authorize]
public sealed class CategoriesModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public sealed record CategoryRow(Guid Id, string Name, string? Description, int SortOrder, bool IsActive, int Services);
    public IReadOnlyList<CategoryRow> Items { get; private set; } = [];
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public int SortOrder { get; set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { if (account.AccountId is not Guid aid) return Forbid(); Items = await db.ServiceCategories.AsNoTracking().Where(x => x.AccountId == aid && !x.IsDeleted).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => new CategoryRow(x.Id,x.Name,x.Description,x.SortOrder,x.IsActive,db.ServiceCatalogItems.Count(s => s.AccountId == aid && s.CategoryId == x.Id && !s.IsDeleted))).Take(100).ToListAsync(ct); return Page(); }
    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct) { if(account.AccountId is not Guid aid)return Forbid();if(string.IsNullOrWhiteSpace(Name)){ModelState.AddModelError(nameof(Name),"Informe o nome.");return await OnGetAsync(ct);}var normalized=Name.Trim().ToUpperInvariant();if(await db.ServiceCategories.AnyAsync(x=>x.AccountId==aid&&x.NormalizedName==normalized&&!x.IsDeleted,ct)){ModelState.AddModelError(nameof(Name),"Categoria já cadastrada.");return await OnGetAsync(ct);}db.Add(new ServiceCategory{AccountId=aid,Name=Name.Trim(),NormalizedName=normalized,Description=Description?.Trim(),SortOrder=SortOrder});await db.SaveChangesAsync(ct);return RedirectToPage();}
    public async Task<IActionResult> OnPostToggleAsync(Guid id,CancellationToken ct){if(account.AccountId is not Guid aid)return Forbid();var category=await db.ServiceCategories.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==aid&&!x.IsDeleted,ct);if(category is null)return NotFound();category.IsActive=!category.IsActive;category.Touch();await db.SaveChangesAsync(ct);return RedirectToPage();}
}
