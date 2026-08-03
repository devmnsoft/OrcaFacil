using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Clients;
using OrcaFacil.Web.ViewModels.Clients;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public sealed class CreateModel(IClientWorkspaceService workspace) : PageModel
{
    [BindProperty] public ClientEditorInput Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var i=Input; var result = await workspace.CreateAsync(new(i.PersonType,i.DocumentType,i.DocumentNumber,i.Name,i.LegalName,i.TradeName,i.Email,i.Phone,i.City,i.Address,i.InternalNotes,i.PreferredContactChannel,i.NextFollowUpAt,i.IsFavorite,i.IsActive,i.AllowPossibleDuplicate), ct);
        if (result.Code == ClientResultCode.Success) { TempData["Success"] = "Cliente salvo com sucesso."; return RedirectToPage("Details", new { id = result.ClientId }); }
        ModelState.AddModelError(string.Empty, result.Message ?? "Não foi possível salvar o cliente.");
        return Page();
    }
}
