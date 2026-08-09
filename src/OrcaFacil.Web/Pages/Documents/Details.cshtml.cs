using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class DetailsModel(ICommercialWorkspaceQueryService workspace, ICurrentAccountService account,
    DocumentService documents, ICommercialJourneyService journey) : PageModel
{
    public CommercialDocumentWorkspaceView Document { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Document = (await workspace.GetAsync(id, ct))!;
        return Document is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        if (await workspace.GetAsync(id, ct) is null) return NotFound();
        await documents.DeleteAsync(new(account.UserId, id), ct);
        return RedirectToPage("/Documents/Index");
    }

    public async Task<IActionResult> OnPostDuplicateAsync(Guid id, CancellationToken ct)
    {
        if (await workspace.GetAsync(id, ct) is null) return NotFound();
        var result = await documents.DuplicateAsync(new(account.UserId, id), ct);
        if (!result.Succeeded || result.Value == Guid.Empty)
        {
            TempData["Error"] = result.Error ?? "Não foi possível duplicar o orçamento.";
            return RedirectToPage(new { id });
        }

        TempData["Success"] = "Orçamento duplicado com sucesso.";
        return RedirectToPage("/Documents/Edit", new { id = result.Value });
    }

    public async Task<IActionResult> OnPostPublicLinkAsync(Guid id, CancellationToken ct)
    {
        if (await workspace.GetAsync(id, ct) is null) return NotFound();
        var result = await journey.CreatePublicAccessAsync(id, TimeSpan.FromDays(30), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.PublicToken))
            TempData["PublicLink"] = Url.PageLink("/PublicQuotes/View", values: new { token = result.PublicToken });
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostWorkOrderAsync(Guid id, CancellationToken ct)
    {
        var result = await journey.ConvertToWorkOrderAsync(id, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRevisionAsync(Guid id, CancellationToken ct)
    {
        var result = await journey.CreateRevisionAsync(id, "essential", ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostScheduleFollowUpAsync(Guid id, DateTime? nextFollowUpAt, string? followUpNote, CancellationToken ct)
    {
        var result = await journey.ScheduleFollowUpAsync(new(id, nextFollowUpAt, followUpNote), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSnoozeFollowUpAsync(Guid id, DateTime? nextFollowUpAt, string? followUpNote, CancellationToken ct)
    {
        var result = await journey.SnoozeFollowUpAsync(new(id, nextFollowUpAt, followUpNote), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteFollowUpAsync(Guid id, string? followUpNote, CancellationToken ct)
    {
        var result = await journey.CompleteFollowUpAsync(id, followUpNote, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }
}
