using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.WorkOrders;

[Authorize]
public sealed class DetailsModel(OrcaFacilDbContext db, ICurrentAccountService account, ICommercialJourneyService journey) : PageModel
{
    public WorkOrder? Order { get; private set; }
    public IReadOnlyList<WorkOrderChecklistItem> Checklist { get; private set; } = [];
    public IReadOnlyList<ManualPayment> Payments { get; private set; } = [];
    public IReadOnlyList<Receipt> Receipts { get; private set; } = [];
    public IReadOnlyList<ActivityEvent> Timeline { get; private set; } = [];

    [BindProperty, Required, StringLength(240, MinimumLength = 3)]
    public string NewItemDescription { get; set; } = string.Empty;

    [BindProperty, StringLength(1000)]
    public string? NewItemDetails { get; set; }

    [BindProperty]
    public bool NewItemIsRequired { get; set; } = true;

    [BindProperty, StringLength(1000)]
    public string? CompletionNote { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) => await LoadAsync(id, ct) ? Page() : NotFound();

    public async Task<IActionResult> OnPostStartAsync(Guid id, CancellationToken ct)
    {
        var result = await journey.StartAsync(id, ct); TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id, CancellationToken ct)
    {
        var incomplete = await db.WorkOrderChecklistItems.AnyAsync(x => x.AccountId == account.AccountId && x.WorkOrderId == id && !x.IsDeleted && x.IsRequired && !x.IsCompleted, ct);
        if (incomplete) { TempData["Error"] = "Conclua os itens obrigatórios do checklist antes de finalizar a ordem."; return RedirectToPage(new { id }); }
        var result = await journey.CompleteAsync(id, null, ct); TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddChecklistAsync(Guid id, CancellationToken ct)
    {
        if (!ModelState.IsValid || !await OwnsOrderAsync(id, ct)) { TempData["Error"] = "Informe uma tarefa válida."; return RedirectToPage(new { id }); }
        var position = (await db.WorkOrderChecklistItems.Where(x => x.AccountId == account.AccountId && x.WorkOrderId == id).MaxAsync(x => (int?)x.Position, ct) ?? 0) + 1;
        db.WorkOrderChecklistItems.Add(new WorkOrderChecklistItem { AccountId = account.AccountId!.Value, WorkOrderId = id, Description = NewItemDescription.Trim(), Details = NewItemDetails?.Trim(), IsRequired = NewItemIsRequired, Position = position });
        await db.SaveChangesAsync(ct); TempData["Success"] = "Tarefa adicionada ao checklist.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEditChecklistAsync(Guid id, Guid itemId, string description, string? details, bool isRequired, CancellationToken ct)
    {
        var item = await FindChecklistItemAsync(id, itemId, ct);
        if (item is null) return NotFound();
        description = description?.Trim() ?? string.Empty;
        if (description.Length is < 3 or > 240 || details?.Length > 1000) { TempData["Error"] = "Informe um título entre 3 e 240 caracteres."; return RedirectToPage(new { id }); }
        item.Description = description; item.Details = details?.Trim(); item.IsRequired = isRequired; item.Touch();
        db.ActivityEvents.Add(NewChecklistEvent(id, "WorkOrderChecklistEdited", $"Checklist atualizado: {item.Description}."));
        await db.SaveChangesAsync(ct); TempData["Success"] = "Item do checklist atualizado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveChecklistAsync(Guid id, Guid itemId, CancellationToken ct)
    {
        var item = await FindChecklistItemAsync(id, itemId, ct);
        if (item is null) return NotFound();
        item.MarkAsDeleted();
        db.ActivityEvents.Add(NewChecklistEvent(id, "WorkOrderChecklistRemoved", $"Checklist removido: {item.Description}."));
        await db.SaveChangesAsync(ct); TempData["Success"] = "Item removido do checklist.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleChecklistAsync(Guid id, Guid itemId, CancellationToken ct)
    {
        var item = await db.WorkOrderChecklistItems.SingleOrDefaultAsync(x => x.Id == itemId && x.WorkOrderId == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
        if (item is null) return NotFound();
        item.IsCompleted = !item.IsCompleted; item.CompletedAt = item.IsCompleted ? DateTime.UtcNow : null;
        item.CompletedByUserId = item.IsCompleted ? account.UserId : null; item.CompletionNote = item.IsCompleted ? CompletionNote?.Trim() : null; item.Touch();
        db.ActivityEvents.Add(NewChecklistEvent(id, item.IsCompleted ? "WorkOrderChecklistCompleted" : "WorkOrderChecklistReopened", item.IsCompleted ? $"Checklist concluído: {item.Description}." : $"Checklist reaberto: {item.Description}."));
        await db.SaveChangesAsync(ct); TempData["Success"] = item.IsCompleted ? "Tarefa concluída e registrada na timeline." : "Tarefa reaberta.";
        return RedirectToPage(new { id });
    }

    private Task<bool> OwnsOrderAsync(Guid id, CancellationToken ct) => db.WorkOrders.AnyAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);

    private Task<WorkOrderChecklistItem?> FindChecklistItemAsync(Guid id, Guid itemId, CancellationToken ct) =>
        db.WorkOrderChecklistItems.SingleOrDefaultAsync(x => x.Id == itemId && x.WorkOrderId == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);

    private ActivityEvent NewChecklistEvent(Guid id, string action, string summary) => new()
    {
        AccountId = account.AccountId, ActorUserId = account.UserId, Action = action,
        EntityType = "CommercialJourney", EntityId = id, Summary = summary
    };

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        Order = await db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
        if (Order is null) return false;
        Checklist = await db.WorkOrderChecklistItems.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.WorkOrderId == id && !x.IsDeleted).OrderBy(x => x.Position).ToListAsync(ct);
        Payments = await db.ManualPayments.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.WorkOrderId == id && !x.IsDeleted).OrderByDescending(x => x.PaidAt).ToListAsync(ct);
        Receipts = await db.Receipts.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.WorkOrderId == id && !x.IsDeleted).OrderByDescending(x => x.IssuedAt).ToListAsync(ct);
        Timeline = await db.ActivityEvents.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.EntityId == id && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(30).ToListAsync(ct);
        return true;
    }
}
