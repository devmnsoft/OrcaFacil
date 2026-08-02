using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Services;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public static IReadOnlyDictionary<string,string> Units { get; } = ServiceFormModel.Units;
    [BindProperty(SupportsGet=true)] public string? Search { get; set; }
    [BindProperty(SupportsGet=true)] public string? Unit { get; set; }
    [BindProperty(SupportsGet=true)] public string Status { get; set; } = "active";
    public List<ServiceCatalogItem> Items { get; private set; } = [];
    public int ActiveCount { get; private set; } public int InactiveCount { get; private set; } public int FavoriteCount { get; private set; } public int UsedCount { get; private set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        var all = db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        ActiveCount=await all.CountAsync(x=>x.IsActive,ct); InactiveCount=await all.CountAsync(x=>!x.IsActive,ct); FavoriteCount=await all.CountAsync(x=>x.IsFavorite,ct); UsedCount=await all.CountAsync(x=>x.UseCount>0,ct);
        var query=all; if(Status!="all") query=query.Where(x=>x.IsActive==(Status!="inactive")); if(!string.IsNullOrWhiteSpace(Unit)) query=query.Where(x=>x.UnitCode==Unit); if(!string.IsNullOrWhiteSpace(Search)){var term=$"%{Search.Trim()}%";query=query.Where(x=>EF.Functions.ILike(x.Name,term)||(x.Code!=null&&EF.Functions.ILike(x.Code,term))||(x.Description!=null&&EF.Functions.ILike(x.Description,term)));}
        Items=await query.OrderByDescending(x=>x.IsFavorite).ThenBy(x=>x.Name).ToListAsync(ct); return Page();
    }
    public async Task<IActionResult> OnPostFavoriteAsync(Guid id,CancellationToken ct){if(account.AccountId is not Guid accountId)return Forbid();var item=await db.ServiceCatalogItems.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==accountId&&!x.IsDeleted,ct);if(item is null)return NotFound();item.IsFavorite=!item.IsFavorite;item.Touch();await db.SaveChangesAsync(ct);return RedirectToPage();}
}
