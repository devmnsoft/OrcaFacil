using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Quality;
using OrcaFacil.Application.Security;

namespace OrcaFacil.Web.Pages.Admin.JourneyRefinement;

[Authorize(Policy = "Permission:" + PermissionCodes.JourneyRefinementView)]
public sealed class IndexModel(UserJourneyReviewService reviews, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<JourneyRefinementResult> Journeys { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await account.HasPermissionAsync(PermissionCodes.JourneyRefinementView, cancellationToken)) return Forbid();
        Journeys = reviews.Review(DateTimeOffset.UtcNow);
        return Page();
    }
}
