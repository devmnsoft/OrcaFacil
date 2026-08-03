using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Clients;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public sealed class EditModel(IClientWorkspaceService workspace) : PageModel
{
    [BindProperty]
    public Client Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var details = await workspace.GetDetailsAsync(id, ct);
        if (details is null)
            return NotFound();

        Input = details.Client;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await workspace.UpdateAsync(id, Input, false, ct);
        if (result.Code == ClientResultCode.ClientNotFound)
            return NotFound();
        if (result.Code == ClientResultCode.AccessDenied)
            return Forbid();
        if (!result.Succeeded())
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Não foi possível salvar o cliente.");
            return Page();
        }

        TempData["Success"] = "Cliente salvo com sucesso.";
        return RedirectToPage("/Clients/Details", new { id });
    }
}

internal static class ClientSaveResultExtensions
{
    public static bool Succeeded(this ClientSaveResult result) => result.Code == ClientResultCode.Success;
}
