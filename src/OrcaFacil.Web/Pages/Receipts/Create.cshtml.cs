using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.ViewModels.Receipts;

namespace OrcaFacil.Web.Pages.Receipts;

[Authorize]
public sealed class CreateModel(IReceiptApplicationService receipts, ICurrentAccountService account, OrcaFacilDbContext db) : PageModel
{
    [BindProperty] public ReceiptCreateInput Input { get; set; } = new();
    public IReadOnlyList<SelectListItem> Clients { get; private set; } = [];
    public IReadOnlyList<SelectListItem> WorkOrders { get; private set; } = [];
    public IReadOnlyList<SelectListItem> Budgets { get; private set; } = [];
    public IReadOnlyList<PaymentMethodOption> PaymentMethods { get; } =
    [
        new("pix", "Pix", "pix"),
        new("cash", "Dinheiro", "cash"),
        new("transfer", "Transferência", "transfer"),
        new("card", "Cartão", "card"),
        new("boleto", "Boleto", "boleto"),
        new("other", "Outro", "payment")
    ];

    public async Task<IActionResult> OnGetAsync(Guid? clientId, Guid? workOrderId, Guid? paymentId, Guid? documentId, CancellationToken ct)
    {
        if (!account.HasAccount) return Forbid();
        await LoadOptionsAsync(ct);
        Input.ClientId = clientId ?? Guid.Empty; Input.WorkOrderId = workOrderId; Input.DocumentId = documentId;
        Input.OriginType = workOrderId.HasValue ? ReceiptOriginType.WorkOrder : documentId.HasValue ? ReceiptOriginType.Budget : ReceiptOriginType.Standalone;
        if (workOrderId is Guid orderId)
        {
            var order = await db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId && x.AccountId == account.AccountId && !x.IsDeleted, ct);
            if (order is not null) { Input.ClientId = order.ClientId; Input.Amount = order.TotalSnapshot; Input.ServiceDescription = order.Title; }
        }
        if (documentId is Guid budgetId)
        {
            var budget = await db.Documents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == budgetId && x.AccountId == account.AccountId && !x.IsDeleted, ct);
            if (budget is not null) { Input.ClientId = budget.ClientId ?? Guid.Empty; Input.Amount = budget.Total; Input.ServiceDescription = $"Orçamento {budget.Number}"; }
        }
        if (paymentId is Guid existingPayment)
        {
            var payment = await db.ManualPayments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == existingPayment && x.AccountId == account.AccountId && !x.IsDeleted, ct);
            if (payment is not null) { Input.PaymentId = payment.Id; Input.ClientId = payment.ClientId; Input.WorkOrderId = payment.WorkOrderId; Input.DocumentId = payment.DocumentId; Input.Amount = payment.Amount; Input.PaymentMethod = payment.PaymentMethod; Input.PaidAt = payment.PaidAt; Input.OriginType = payment.WorkOrderId.HasValue ? ReceiptOriginType.WorkOrder : payment.DocumentId.HasValue ? ReceiptOriginType.Budget : ReceiptOriginType.Standalone; }
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (Input.PaidAt.Date > DateTime.UtcNow.Date) ModelState.AddModelError("Input.PaidAt", "A data não pode estar no futuro.");
        if (!ModelState.IsValid) { await LoadOptionsAsync(ct); return Page(); }
        if (account.AccountId is not Guid accountId) return Forbid();
        if (string.IsNullOrWhiteSpace(Input.IdempotencyKey)) Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        var result = Input.PaymentId is Guid paymentId
            ? await receipts.CreateForPaymentAsync(paymentId, Input.ServiceDescription, Input.City, Input.Notes, ct)
            : await receipts.CreateAsync(new(accountId, Input.ClientId, Input.OriginType, Input.WorkOrderId,
            Input.DocumentId, Input.Amount, Input.PaymentMethod, Input.PaidAt, Input.City,
            Input.ServiceDescription, Input.Notes, Input.IdempotencyKey, Input.DocumentId), ct);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Message); await LoadOptionsAsync(ct); return Page(); }
        TempData["Success"] = result.Message;
        return RedirectToPage(result.RedirectPage, new { id = result.ReceiptId });
    }

    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        var accountId = account.AccountId;
        Clients = await db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync(ct);
        WorkOrders = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt)
            .Select(x => new SelectListItem(x.Number + " · " + x.Title, x.Id.ToString())).Take(30).ToListAsync(ct);
        Budgets = await db.Documents.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget)
            .OrderByDescending(x => x.CreatedAt).Select(x => new SelectListItem(x.Number + " · " + x.ClientName, x.Id.ToString())).Take(30).ToListAsync(ct);
    }
}

public sealed record PaymentMethodOption(string Code, string Label, string IconName);
