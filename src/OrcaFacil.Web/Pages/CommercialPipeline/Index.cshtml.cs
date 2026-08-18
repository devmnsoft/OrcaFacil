using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Commercial;

namespace OrcaFacil.Web.Pages.CommercialPipeline;

[Authorize]
public sealed class IndexModel(ICommercialWorkspaceQueryService workspace) : PageModel
{
    public CommercialDashboardView Pipeline { get; private set; } = default!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Pipeline = await workspace.GetDashboardAsync(cancellationToken);
    }
}
