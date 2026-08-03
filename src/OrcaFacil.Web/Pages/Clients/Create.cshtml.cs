using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Clients;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public sealed class CreateModel(IClientWorkspaceService workspace) : PageModel
{
    [BindProperty] public Client Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var result = await workspace.CreateAsync(Input, false, ct);
        if (result.Code == ClientResultCode.Success) { TempData["Success"] = "Cliente salvo com sucesso."; return RedirectToPage("Details", new { id = result.ClientId }); }
        ModelState.AddModelError(string.Empty, result.Message ?? "Não foi possível salvar o cliente.");
        return Page();
    }
}
