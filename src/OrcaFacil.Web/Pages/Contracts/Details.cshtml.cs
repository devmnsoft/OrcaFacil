using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Contracts;

[Authorize]
public sealed class DetailsModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public RecurringContract Contract { get; private set; } = null!;
    public string ClientName { get; private set; } = string.Empty;
    public IReadOnlyList<ContractEvent> Timeline { get; private set; } = [];
    public IReadOnlyList<ContractPayment> Payments { get; private set; } = [];
    public IReadOnlyList<WorkOrder> WorkOrders { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) => await Load(id, ct) ? Page() : NotFound();
    public async Task<IActionResult> OnPostStatusAsync(Guid id, ContractStatus status, string? reason, CancellationToken ct)
    {
        var contract = await Find(id, ct); if (contract is null) return NotFound();
        if (status == ContractStatus.Canceled && string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Informe o motivo do cancelamento."; return RedirectToPage(new { id }); }
        contract.Status = status; if (status == ContractStatus.Active) contract.ActivatedAt ??= DateTime.UtcNow; if (status == ContractStatus.Canceled) { contract.CanceledAt = DateTime.UtcNow; contract.CancellationReason = reason; }
        AddEvent(contract, status.ToString(), status == ContractStatus.Canceled ? $"Contrato cancelado. Motivo: {reason}" : $"Status alterado para {status}."); await db.SaveChangesAsync(ct); return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostPaymentAsync(Guid id, string competence, DateOnly dueDate, decimal amount, CancellationToken ct)
    {
        var contract = await Find(id, ct); if (contract is null) return NotFound();
        if (amount <= 0 || !TryCompetence(competence, out var competenceDate)) { TempData["Error"] = "Revise competência e valor."; return RedirectToPage(new { id }); }
        if (await db.ContractPayments.AnyAsync(x => x.AccountId == account.AccountId && x.ContractId == id && x.Competence == competenceDate && !x.IsDeleted, ct)) { TempData["Error"] = "Já existe pagamento para esta competência."; return RedirectToPage(new { id }); }
        var payment = new ContractPayment { AccountId = contract.AccountId, ContractId = id, ClientId = contract.ClientId, Competence = competenceDate, DueDate = dueDate, Amount = amount, Status = RecurringPaymentStatus.Pending };
        db.Add(payment); AddEvent(contract, "PaymentForecast", $"Pagamento de {amount:C} previsto para {dueDate:dd/MM/yyyy}.", "ContractPayment", payment.Id, $"/Contracts/Details/{id}#payments"); await db.SaveChangesAsync(ct); return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostPayAsync(Guid id, Guid paymentId, string paymentMethod, CancellationToken ct)
    {
        var contract = await Find(id, ct); if (contract is null) return NotFound();
        var payment = await db.ContractPayments.SingleOrDefaultAsync(x => x.Id == paymentId && x.ContractId == id && x.AccountId == account.AccountId && !x.IsDeleted, ct); if (payment is null) return NotFound();
        if (payment.Status == RecurringPaymentStatus.Paid) { TempData["Error"] = "Pagamento já registrado."; return RedirectToPage(new { id }); }
        var manual = new ManualPayment { AccountId = contract.AccountId, ClientId = contract.ClientId, Amount = payment.Amount, PaymentMethod = paymentMethod, PaidAt = DateTime.UtcNow, Notes = $"Contrato {contract.Number}; competência {payment.Competence:MM/yyyy}", RegisteredByUserId = account.UserId, IdempotencyKey = $"contract:{payment.Id}" };
        db.Add(manual); payment.Status = RecurringPaymentStatus.Paid; payment.PaidAt = manual.PaidAt; payment.PaymentMethod = paymentMethod; payment.ManualPaymentId = manual.Id;
        AddEvent(contract, "PaymentPaid", $"Pagamento de {payment.Amount:C} registrado.", "ManualPayment", manual.Id, $"/Receipts/Create?paymentId={manual.Id}"); await db.SaveChangesAsync(ct); return RedirectToPage("/Receipts/Create", new { paymentId = manual.Id });
    }
    public async Task<IActionResult> OnPostWorkOrderAsync(Guid id, string competence, CancellationToken ct)
    {
        var contract = await db.RecurringContracts.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct); if (contract is null) return NotFound();
        if (!TryCompetence(competence, out var competenceDate)) { TempData["Error"] = "Informe uma competência válida."; return RedirectToPage(new { id }); }
        if (await db.WorkOrders.AnyAsync(x => x.AccountId == account.AccountId && x.ContractId == id && x.ServiceCompetence == competenceDate && !x.IsDeleted, ct)) { TempData["Error"] = "A OS desta competência já existe."; return RedirectToPage(new { id }); }
        var client = await db.Clients.AsNoTracking().SingleAsync(x => x.Id == contract.ClientId && x.AccountId == account.AccountId, ct); var count = await db.WorkOrders.CountAsync(x => x.AccountId == account.AccountId, ct) + 1;
        var order = new WorkOrder { AccountId = contract.AccountId, ContractId = contract.Id, ServiceCompetence = competenceDate, ClientId = contract.ClientId, Number = $"OS-{DateTime.UtcNow:yyyy}-{count:0000}", Title = $"{contract.Title} · {competenceDate:MM/yyyy}", Description = contract.Description, ScheduledStart = competenceDate.ToDateTime(TimeOnly.MinValue), AssignedUserId = contract.ResponsibleUserId, AddressSnapshot = JsonSerializer.Serialize(new { client.Address, client.City }), ClientSnapshot = JsonSerializer.Serialize(new { client.Name, client.Email, client.Phone }), ItemsSnapshot = JsonSerializer.Serialize(contract.Items.Select(x => new { x.Description, x.Quantity, x.UnitPrice })), TotalSnapshot = contract.RecurringAmount, CreatedByUserId = account.UserId };
        db.Add(order); AddEvent(contract, "WorkOrderCreated", $"OS {order.Number} gerada para {competenceDate:MM/yyyy}.", "WorkOrder", order.Id, $"/WorkOrders/Details/{order.Id}"); await db.SaveChangesAsync(ct); return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostRenewAsync(Guid id, DateOnly startDate, DateOnly? endDate, decimal amount, CancellationToken ct)
    {
        var old = await db.RecurringContracts.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct); if (old is null) return NotFound();
        if (amount <= 0 || (endDate.HasValue && endDate < startDate)) { TempData["Error"] = "Revise valor e vigência."; return RedirectToPage(new { id }); }
        var count = await db.RecurringContracts.CountAsync(x => x.AccountId == account.AccountId, ct) + 1;
        var renewed = new RecurringContract { AccountId = old.AccountId, ClientId = old.ClientId, ResponsibleUserId = old.ResponsibleUserId, Number = $"CTR-{DateTime.UtcNow:yyyy}-{count:0000}", Title = old.Title, Description = old.Description, StartDate = startDate, EndDate = endDate, Status = ContractStatus.Draft, RecurringAmount = amount, Periodicity = old.Periodicity, DueDay = old.DueDay, NextBillingDate = startDate, NextServiceDate = startDate, CommercialTerms = old.CommercialTerms, CustomerNotes = old.CustomerNotes, InternalNotes = old.InternalNotes, AutoRenew = old.AutoRenew, RenewalNoticeDays = old.RenewalNoticeDays, ResponseSlaHours = old.ResponseSlaHours, ExecutionSlaHours = old.ExecutionSlaHours, RenewedFromContractId = old.Id };
        foreach(var item in old.Items) renewed.Items.Add(new ContractItem { AccountId = old.AccountId, Description = item.Description, Quantity = item.Quantity, UnitPrice = item.UnitPrice, ServiceCatalogItemId = item.ServiceCatalogItemId, Checklist = item.Checklist }); old.Status = ContractStatus.Finished;
        db.Add(renewed); AddEvent(old, "Renewed", $"Contrato renovado como {renewed.Number}.", "RecurringContract", renewed.Id, $"/Contracts/Details/{renewed.Id}"); AddEvent(renewed, "CreatedByRenewal", $"Renovação do contrato {old.Number}.", "RecurringContract", old.Id, $"/Contracts/Details/{old.Id}"); await db.SaveChangesAsync(ct); return RedirectToPage(new { id = renewed.Id });
    }
    private Task<RecurringContract?> Find(Guid id, CancellationToken ct) => db.RecurringContracts.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
    private static bool TryCompetence(string value, out DateOnly result) => DateOnly.TryParseExact(value + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result);
    private void AddEvent(RecurringContract c, string type, string description, string? relatedType = null, Guid? relatedId = null, string? url = null) => db.Add(new ContractEvent { AccountId = c.AccountId, ContractId = c.Id, UserId = account.UserId, Type = type, Description = description, RelatedEntityType = relatedType, RelatedEntityId = relatedId, RelatedUrl = url });
    private async Task<bool> Load(Guid id, CancellationToken ct) { Contract = (await db.RecurringContracts.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct))!; if (Contract is null) return false; ClientName = await db.Clients.Where(x => x.Id == Contract.ClientId && x.AccountId == account.AccountId).Select(x => x.Name).SingleAsync(ct); Timeline = await db.ContractEvents.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.ContractId == id && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(ct); Payments = await db.ContractPayments.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.ContractId == id && !x.IsDeleted).OrderByDescending(x => x.Competence).ToListAsync(ct); WorkOrders = await db.WorkOrders.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.ContractId == id && !x.IsDeleted).OrderByDescending(x => x.ServiceCompetence).ToListAsync(ct); return true; }
}
