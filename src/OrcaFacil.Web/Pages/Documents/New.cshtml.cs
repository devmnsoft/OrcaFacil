using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Documents;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class NewModel(IGuidedBudgetStartService service) : PageModel
{
    public GuidedBudgetStartView Start { get; private set; } = null!;
    public IReadOnlyList<BudgetStartClient> Clients => Start.Clients;
    public IReadOnlyList<BudgetStartService> Services => Start.Services;
    public IReadOnlyList<BudgetStartTemplate> Templates => Start.Templates;
    public IReadOnlyList<BudgetStartDraft> Drafts => Start.Drafts;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Start = await service.GetAsync(ct);
        return Page();
    }
}
