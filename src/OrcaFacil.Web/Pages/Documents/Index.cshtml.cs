using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class IndexModel(IQuoteWorkspaceService quotes) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? Minimum { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? Maximum { get; set; }
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "newest";
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
    public PagedResult<QuoteWorkspaceItem> Quotes { get; private set; } = new([], 0, 1, 20);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Minimum is < 0) ModelState.AddModelError(nameof(Minimum), "Informe um valor mínimo válido.");
        if (Maximum is < 0) ModelState.AddModelError(nameof(Maximum), "Informe um valor máximo válido.");
        if (Minimum.HasValue && Maximum.HasValue && Minimum > Maximum)
            ModelState.AddModelError(nameof(Maximum), "O valor máximo deve ser maior ou igual ao mínimo.");
        if (!ModelState.IsValid) return Page();

        var result = await quotes.ListAsync(new(Search, Status, From: From, To: To, Minimum: Minimum,
            Maximum: Maximum, Sort: Sort, Page: CurrentPage), cancellationToken);
        if (!result.Succeeded || result.Value is null)
            return result.Code == "access_denied" ? Forbid() : StatusCode(StatusCodes.Status503ServiceUnavailable);
        Quotes = result.Value;
        return Page();
    }
}
