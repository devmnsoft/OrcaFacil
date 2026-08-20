using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Search;

[Authorize]
public sealed class IndexModel(IGlobalSearchService search, ICurrentAccountService account) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Query { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public IReadOnlyList<GlobalSearchResult> Results { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!await account.HasPermissionAsync(PermissionCodes.SearchGlobal, ct)) return Forbid();
        PageNumber = Math.Max(1, PageNumber);
        if (!string.IsNullOrWhiteSpace(Query) && Query.Trim().Length >= 2)
            Results = await search.SearchAsync(Query, 20, ct);
        return Page();
    }
}
