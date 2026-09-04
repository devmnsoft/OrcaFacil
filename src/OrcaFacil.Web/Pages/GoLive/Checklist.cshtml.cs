using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence.Services.GoLive;

namespace OrcaFacil.Web.Pages.GoLive;

[Authorize]
public sealed class ChecklistModel(GoLivePersistenceService service, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<GoLiveChecklistItem> Items { get; private set; } = [];
    public int Progress => Items.Count == 0 ? 0 : (int)Math.Round(Items.Count(x=>x.IsCompleted) * 100m / Items.Count);
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { if (account.AccountId is not Guid id) return Forbid(); Items = await service.GetOrCreateAsync(id,ct); return Page(); }
    public async Task<IActionResult> OnPostCompleteAsync(Guid itemId, string responsible, string observation, bool confirmed, CancellationToken ct)
    {
        if (account.AccountId is not Guid id) return Forbid();
        try { await service.CompleteManualAsync(id,itemId,account.UserId,responsible,observation,confirmed,ct); TempData["Success"]="Verificação registrada com auditoria."; }
        catch (InvalidOperationException e) { ModelState.AddModelError(string.Empty,e.Message); }
        Items = await service.GetOrCreateAsync(id,ct); return Page();
    }
}
