using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Security;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public sealed class DetailsModel(ICommercialWorkspaceQueryService workspace, ICurrentAccountService account,
    DocumentService documents, ICommercialJourneyService journey, BudgetWizardService wizard,
    OrcaFacilDbContext db, IAuditService audit) : PageModel
{
    public CommercialDocumentWorkspaceView Document { get; private set; } = null!;

    public async Task<IActionResult> OnPostSaveAsTemplateAsync(Guid id, string templateTitle, CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        try { await account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var canManage = await account.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) ||
                        await account.HasPermissionAsync(PermissionCodes.DocumentTemplatesManage, ct) ||
                        await account.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct);
        if (!canManage)
        {
            TempData["Error"] = "Você não possui permissão para criar modelos de orçamento.";
            return RedirectToPage(new { id });
        }

        if (string.IsNullOrWhiteSpace(templateTitle))
        {
            TempData["Error"] = "Informe o nome do modelo.";
            return RedirectToPage(new { id });
        }

        if (templateTitle.Trim().Length > 120)
        {
            TempData["Error"] = "O nome do modelo aceita no máximo 120 caracteres.";
            return RedirectToPage(new { id });
        }

        if (await workspace.GetAsync(id, ct) is null) return NotFound();

        var result = await wizard.SaveAsTemplateAsync(account.UserId, account.AccountId, id, templateTitle, ct);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message ?? "Não foi possível criar o modelo.";
            return RedirectToPage(new { id });
        }

        await audit.RegisterAsync(account.UserId, "CreateFromDocument", "BudgetTemplate", result.Value.ToString(), null, new { DocumentId = id, Title = templateTitle }, null, ct, account.AccountId);

        TempData["Success"] = "Modelo salvo com sucesso a partir deste orçamento!";
        return RedirectToPage("/Templates/Details", new { id = result.Value });
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid id, Guid? assigneeUserId, CancellationToken ct)
    {
        if (account.AccountId is not Guid accountId) return Forbid();
        try { await account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var canEdit = await account.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) ||
                      await account.HasPermissionAsync(PermissionCodes.CommercialActionsManage, ct);
        if (!canEdit)
        {
            TempData["Error"] = "Você não possui permissão para alterar o responsável pelo orçamento.";
            return RedirectToPage(new { id });
        }

        var doc = await db.Documents.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId && !x.IsDeleted, ct);
        if (doc is null) return NotFound();

        if (assigneeUserId.HasValue)
        {
            var isMember = await db.AccountMembers.AnyAsync(m => m.AccountId == accountId && m.UserId == assigneeUserId.Value && !m.IsDeleted, ct);
            if (!isMember)
            {
                TempData["Error"] = "O responsável selecionado não é membro ativo desta conta.";
                return RedirectToPage(new { id });
            }
        }

        doc.AssignedToUserId = assigneeUserId;
        doc.Touch();
        await db.SaveChangesAsync(ct);

        TempData["Success"] = "Responsável comercial atualizado com sucesso.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Document = (await workspace.GetAsync(id, ct))!;
        return Document is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        if (await workspace.GetAsync(id, ct) is null) return NotFound();
        await documents.DeleteAsync(new(account.UserId, id, account.AccountId), ct);
        return RedirectToPage("/Documents/Index");
    }

    public async Task<IActionResult> OnPostDuplicateAsync(Guid id, CancellationToken ct)
    {
        if (await workspace.GetAsync(id, ct) is null) return NotFound();
        var result = await documents.DuplicateAsync(new(account.UserId, id, account.AccountId), ct);
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
