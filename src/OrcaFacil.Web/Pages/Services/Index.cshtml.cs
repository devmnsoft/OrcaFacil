using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Services;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Web.Pages.Services;
[Authorize]
public sealed class IndexModel(IServiceCatalogApplicationService catalog) : PageModel
{
    public static IReadOnlyDictionary<string,string> Units { get; } = ServiceFormModel.Units;
    [BindProperty(SupportsGet=true)] public string? Search { get; set; }
    [BindProperty(SupportsGet=true)] public string? Unit { get; set; }
    [BindProperty(SupportsGet=true)] public string Status { get; set; } = "active";
    [BindProperty(SupportsGet=true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet=true)] public int PageSize { get; set; } = 20;
    public List<ServiceCatalogItem> Items { get; private set; } = [];
    public int ActiveCount { get; private set; } public int InactiveCount { get; private set; } public int FavoriteCount { get; private set; } public int UsedCount { get; private set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { var result = await catalog.ListAsync(new(Search, Unit: Unit, Active: Status == "all" ? null : Status != "inactive", Page: PageNumber, PageSize: PageSize), ct); if (result is null) return Forbid(); Items = result.Items.ToList(); ActiveCount=result.Active;InactiveCount=result.Inactive;FavoriteCount=result.Favorites;UsedCount=result.Used;return Page(); }
    public async Task<IActionResult> OnPostFavoriteAsync(Guid id,CancellationToken ct) { var result=await catalog.ToggleFavoriteAsync(id,ct); return result.Code == ServiceCatalogResultCode.NotFound ? NotFound() : RedirectToPage(); }
}
