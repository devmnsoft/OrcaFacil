using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Clients;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public sealed class DetailsModel(IClientWorkspaceService workspace, ICommercialJourneyService journey) : PageModel
{
    public ClientWorkspaceDetails Details { get; private set; } = null!;
    public string MaskedDocument => Details.Client.MaskedDocument;

    [BindProperty] public ClientContactInput Contact { get; set; } = new("", ClientContactType.Email, "", null, false, false, false);
    [BindProperty] public string TagName { get; set; } = "";
    [BindProperty] public string TagColor { get; set; } = "neutral";
    [BindProperty] public string NoteContent { get; set; } = "";
    [BindProperty] public bool NotePinned { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) => await Load(id, ct) ? Page() : NotFound();

    public async Task<IActionResult> OnPostContactAsync(Guid id, CancellationToken ct)
    {
        var result = await workspace.AddContactAsync(id, Contact, ct);
        if (result.Code != ClientResultCode.Success) TempData["Error"] = result.Message ?? "Não foi possível adicionar o contato.";
        return RedirectToPage(new { id, tab = "contacts" });
    }

    public async Task<IActionResult> OnPostRemoveContactAsync(Guid id, Guid contactId, CancellationToken ct)
    {
        var result = await workspace.RemoveContactAsync(id, contactId, ct);
        if (result.Code != ClientResultCode.Success) TempData["Error"] = result.Message ?? "Não foi possível remover o contato.";
        return RedirectToPage(new { id, tab = "contacts" });
    }

    public async Task<IActionResult> OnPostTagAsync(Guid id, CancellationToken ct)
    {
        var result = await workspace.CreateAndAssignTagAsync(id, TagName, TagColor, ct);
        if (result.Code != ClientResultCode.Success) TempData["Error"] = result.Message ?? "Não foi possível atribuir a tag.";
        return RedirectToPage(new { id, tab = "notes" });
    }

    public async Task<IActionResult> OnPostRemoveTagAsync(Guid id, Guid tagId, CancellationToken ct)
    {
        await workspace.RemoveTagAsync(id, tagId, ct);
        return RedirectToPage(new { id, tab = "notes" });
    }

    public async Task<IActionResult> OnPostNoteAsync(Guid id, CancellationToken ct)
    {
        var result = await workspace.AddNoteAsync(id, NoteContent, NotePinned, ct);
        if (result.Code != ClientResultCode.Success) TempData["Error"] = result.Message ?? "Não foi possível salvar a observação.";
        return RedirectToPage(new { id, tab = "notes" });
    }

    public async Task<IActionResult> OnPostPinNoteAsync(Guid id, Guid noteId, CancellationToken ct)
    { await workspace.ToggleNotePinAsync(id, noteId, ct); return RedirectToPage(new { id, tab = "notes" }); }
    public async Task<IActionResult> OnPostDeleteNoteAsync(Guid id, Guid noteId, CancellationToken ct)
    { await workspace.DeleteNoteAsync(id, noteId, ct); return RedirectToPage(new { id, tab = "notes" }); }

    public async Task<IActionResult> OnPostScheduleFollowUpAsync(Guid id, Guid documentId, DateTime? nextFollowUpAt, string? note, CancellationToken ct)
    {
        var result = await journey.ScheduleFollowUpAsync(new(documentId, nextFollowUpAt, note), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id, tab = "activities" });
    }

    public async Task<IActionResult> OnPostCompleteFollowUpAsync(Guid id, Guid documentId, string? note, CancellationToken ct)
    {
        var result = await journey.CompleteFollowUpAsync(documentId, note, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id, tab = "activities" });
    }

    private async Task<bool> Load(Guid id, CancellationToken ct)
    { var details = await workspace.GetDetailsAsync(id, ct); if (details is null) return false; Details = details; return true; }
}
