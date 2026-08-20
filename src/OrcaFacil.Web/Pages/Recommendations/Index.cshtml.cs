using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Recommendations;

[Authorize(Policy = "Permission:" + PermissionCodes.RecommendationsView)]
public sealed class IndexModel(IRecommendationService service) : PageModel
{
    public IReadOnlyList<RecommendationCard> Cards { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) => Cards = await service.GetOpenAsync(ct);
}
