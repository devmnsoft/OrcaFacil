using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Clients;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public sealed class IndexModel(IClientWorkspaceService workspace) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public PersonType? PersonType { get; set; }
    [BindProperty(SupportsGet = true)] public string? City { get; set; }
    [BindProperty(SupportsGet = true)] public bool? Favorite { get; set; }
    [BindProperty(SupportsGet = true)] public bool? Active { get; set; }
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "name";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;
    public ClientWorkspaceResult Result { get; private set; } = new(ClientResultCode.Success, [], 0, 0, 0, 0, 0, 1, 20);
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || PersonType.HasValue || !string.IsNullOrWhiteSpace(City) || Favorite.HasValue || Active.HasValue;

    public async Task OnGetAsync(CancellationToken ct) => Result = await workspace.ListAsync(new(Search, PersonType, City, Favorite: Favorite, Active: Active, Sort: Sort, Page: PageNumber, PageSize: PageSize), ct);
    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) { var result = await workspace.DeleteAsync(id, ct); TempData[result.Code == ClientResultCode.Success ? "Success" : "Error"] = result.Code == ClientResultCode.Success ? "Cliente removido. Orçamentos e histórico foram preservados." : "Cliente não encontrado nesta conta."; return RedirectToPage(); }
    public async Task<IActionResult> OnPostFavoriteAsync(Guid id, CancellationToken ct) { await workspace.ToggleFavoriteAsync(id, ct); return RedirectToPage(); }
    public static string Mask(BrazilianDocumentType? type, string? number) => BrazilianDocument.Mask(type, number);
}
