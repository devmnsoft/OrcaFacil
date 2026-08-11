using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Plans;

namespace OrcaFacil.Web.Pages;
public sealed class PrecosModel(IPlanCatalogService catalog) : PageModel
{
    public PlanCatalogView Catalog { get; private set; } = default!;
    public async Task OnGetAsync(CancellationToken ct) => Catalog = await catalog.GetPublishedAsync(ct);
}
