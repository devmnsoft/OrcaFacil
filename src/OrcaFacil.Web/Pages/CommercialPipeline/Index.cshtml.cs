using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.CommercialPipeline;

[Authorize]
public sealed class IndexModel(ICommercialWorkspaceQueryService workspace, ICurrentAccountService account, IPlanAccessService plans) : PageModel
{
    public CommercialDashboardView Pipeline { get; private set; } = default!;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (account.AccountId is { } id)
        {
            var decision = await plans.CanUseAsync(id, PlanFeatureCodes.CommercialPipelineEnabled, cancellationToken);
            if (!decision.IsAllowed)
            {
                TempData.Warning("O pipeline comercial está disponível a partir do plano Negócio. Conheça os planos.");
                return RedirectToPage("/Subscription/Index");
            }
        }
        Pipeline = await workspace.GetDashboardAsync(cancellationToken);
        return Page();
    }
}
