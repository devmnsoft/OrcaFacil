using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Contracts;

[Authorize]
public sealed class CreateModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public IReadOnlyList<Client> Clients { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(Guid? sourceDocumentId, CancellationToken ct)
    {
        await LoadClients(ct); Input.SourceDocumentId = sourceDocumentId;
        if (sourceDocumentId is { } source)
        {
            var existing = await db.RecurringContracts.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == account.AccountId && x.SourceDocumentId == source && !x.IsDeleted, ct);
            if (existing is not null) return RedirectToPage("Details", new { id = existing.Id });
            var document = await db.Documents.Include(x => x.Items).SingleOrDefaultAsync(x => x.AccountId == account.AccountId && x.Id == source && !x.IsDeleted, ct);
            if (document is null || (document.ClientDecision != OrcaFacil.Domain.Enums.ClientDecision.Approved && document.Status != "Approved")) return BadRequest("Somente uma proposta aprovada pode originar contrato.");
            Input.ClientId = document.ClientId; Input.Title = $"Serviços da proposta {document.Number}"; Input.RecurringAmount = document.Total; Input.CommercialTerms = document.ConditionsText;
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadClients(ct);
        if (!await db.Clients.AnyAsync(x => x.AccountId == account.AccountId && x.Id == Input.ClientId && !x.IsDeleted, ct)) ModelState.AddModelError("Input.ClientId", "Cliente inválido.");
        if (Input.EndDate.HasValue && Input.EndDate < Input.StartDate) ModelState.AddModelError("Input.EndDate", "O fim deve ser posterior ao início.");
        Document? source = null;
        if (Input.SourceDocumentId is { } sourceId)
        {
            if (await db.RecurringContracts.AnyAsync(x => x.AccountId == account.AccountId && x.SourceDocumentId == sourceId && !x.IsDeleted, ct)) { ModelState.AddModelError(string.Empty, "Esta proposta já possui contrato. Abra o contrato existente."); return Page(); }
            source = await db.Documents.Include(x => x.Items).SingleOrDefaultAsync(x => x.AccountId == account.AccountId && x.Id == sourceId && !x.IsDeleted, ct);
            if (source is null || source.ClientDecision != OrcaFacil.Domain.Enums.ClientDecision.Approved) ModelState.AddModelError(string.Empty, "A proposta precisa estar aprovada.");
        }
        if (!ModelState.IsValid) return Page();
        var count = await db.RecurringContracts.CountAsync(x => x.AccountId == account.AccountId, ct) + 1;
        var contract = new RecurringContract { AccountId = account.AccountId!.Value, ClientId = Input.ClientId!.Value, SourceDocumentId = Input.SourceDocumentId, ResponsibleUserId = account.UserId, Number = $"CTR-{DateTime.UtcNow:yyyy}-{count:0000}", Title = Input.Title.Trim(), Description = Input.Description, StartDate = Input.StartDate, EndDate = Input.EndDate, RecurringAmount = Input.RecurringAmount, Periodicity = Input.Periodicity, DueDay = Input.DueDay, NextBillingDate = FirstDue(Input.StartDate, Input.DueDay), NextServiceDate = Input.StartDate, CommercialTerms = Input.CommercialTerms, InternalNotes = Input.InternalNotes, CustomerNotes = Input.CustomerNotes, AutoRenew = Input.AutoRenew, RenewalNoticeDays = Input.RenewalNoticeDays, ResponseSlaHours = Input.ResponseSlaHours, ExecutionSlaHours = Input.ExecutionSlaHours };
        if (source is not null) foreach (var item in source.Items) contract.Items.Add(new ContractItem { AccountId = contract.AccountId, Description = item.Description, Quantity = item.Quantity, UnitPrice = item.UnitPrice, ServiceCatalogItemId = item.ServiceCatalogItemId });
        db.Add(contract); db.Add(new ContractEvent { AccountId = contract.AccountId, ContractId = contract.Id, UserId = account.UserId, Type = "Created", Description = source is null ? "Contrato criado." : $"Contrato criado a partir da proposta {source.Number}.", RelatedEntityType = source is null ? null : "Document", RelatedEntityId = source?.Id, RelatedUrl = source is null ? null : $"/Documents/Details/{source.Id}" });
        db.Add(new Notification { AccountId = contract.AccountId, UserId = account.UserId, Title = "Contrato criado", Message = $"{contract.Number} foi criado e aguarda ativação.", ActionUrl = $"/Contracts/Details/{contract.Id}", ActionText = "Abrir contrato" });
        await db.SaveChangesAsync(ct); return RedirectToPage("Details", new { id = contract.Id });
    }
    private Task LoadClients(CancellationToken ct) => db.Clients.AsNoTracking().Where(x => x.AccountId == account.AccountId && x.IsActive && !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(ct).ContinueWith(t => { Clients = t.Result; }, ct);
    private static DateOnly FirstDue(DateOnly start, int day) => new(start.Year, start.Month, Math.Min(day, DateTime.DaysInMonth(start.Year, start.Month)));
    public sealed class InputModel { [Required] public Guid? ClientId { get; set; } public Guid? SourceDocumentId { get; set; } [Required, StringLength(180)] public string Title { get; set; } = string.Empty; [StringLength(2000)] public string? Description { get; set; } public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today); public DateOnly? EndDate { get; set; } [Range(typeof(decimal), "0.01", "999999999")] public decimal RecurringAmount { get; set; } public RecurrencePeriod Periodicity { get; set; } = RecurrencePeriod.Monthly; [Range(1, 28)] public int DueDay { get; set; } = 10; public string? CommercialTerms { get; set; } public string? InternalNotes { get; set; } public string? CustomerNotes { get; set; } public bool AutoRenew { get; set; } [Range(1, 365)] public int RenewalNoticeDays { get; set; } = 30; [Range(1, 8760)] public int? ResponseSlaHours { get; set; } [Range(1, 8760)] public int? ExecutionSlaHours { get; set; } }
}
