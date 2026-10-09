using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Onboarding;

[Authorize]
public sealed class BudgetModel(
    IOnboardingApplicationService onboarding,
    ICurrentAccountService currentAccount,
    BudgetWizardService wizard,
    OrcaFacilDbContext db) : PageModel
{
    public OnboardingStateView State { get; private set; } = null!;
    public IReadOnlyList<Client> AvailableClients { get; private set; } = [];
    public IReadOnlyList<ServiceCatalogItem> AvailableServices { get; private set; } = [];

    [BindProperty]
    public Guid? SelectedClientId { get; set; }

    [BindProperty]
    public Guid? SelectedServiceId { get; set; }

    public string? ClientName { get; private set; }
    public string? ServiceName { get; private set; }
    public decimal ServicePrice { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (currentAccount.AccountId is not Guid accountId) return Forbid();
        try { await currentAccount.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var stateResult = await onboarding.GetAsync(ct);
        if (!stateResult.Succeeded || stateResult.Value is null) return RedirectToPage("Business");
        State = stateResult.Value;

        await LoadOptionsAsync(accountId, ct);

        SelectedClientId ??= AvailableClients.FirstOrDefault()?.Id;
        SelectedServiceId ??= AvailableServices.FirstOrDefault()?.Id;

        UpdateSelectedLabels();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (currentAccount.AccountId is not Guid accountId) return Forbid();
        try { await currentAccount.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var startResult = await onboarding.StartBudgetAsync(ct);
        if (!startResult.Succeeded)
        {
            TempData["Error"] = startResult.Message ?? "Não foi possível avançar para a etapa de orçamento.";
            return RedirectToPage();
        }

        await LoadOptionsAsync(accountId, ct);

        var client = SelectedClientId.HasValue
            ? AvailableClients.FirstOrDefault(x => x.Id == SelectedClientId.Value)
            : AvailableClients.FirstOrDefault();

        var service = SelectedServiceId.HasValue
            ? AvailableServices.FirstOrDefault(x => x.Id == SelectedServiceId.Value)
            : AvailableServices.FirstOrDefault();

        var seedServices = service is not null ? new[] { service.Id } : Array.Empty<Guid>();
        var idempotencyKey = $"onboarding-{accountId:N}";

        // Se o orçamento dessa chave já foi emitido/finalizado, redireciona diretamente
        var existingDoc = await db.Documents.AsNoTracking().FirstOrDefaultAsync(
            d => d.AccountId == accountId && d.LastAutosaveKey == idempotencyKey && !d.IsDeleted, ct);

        if (existingDoc is not null && !string.Equals(existingDoc.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPage("/Documents/Details", new { id = existingDoc.Id });
        }

        var openResult = await wizard.OpenAsync(
            currentAccount.UserId,
            accountId,
            documentId: existingDoc?.Id,
            clientId: client?.Id,
            ct: ct,
            serviceIds: seedServices,
            templateId: null,
            idempotencyKey: idempotencyKey);

        if (openResult.Succeeded && openResult.Draft is not null)
        {
            return RedirectToPage("/Documents/CreateBudget", new { id = openResult.Draft.DocumentId });
        }

        TempData["Error"] = openResult.Error ?? "Não foi possível preparar o rascunho de orçamento.";
        return RedirectToPage("/Documents/New");
    }

    public async Task<IActionResult> OnPostDoneAsync(CancellationToken ct)
    {
        var r = await onboarding.CompleteAsync(ct);
        return r.Succeeded ? RedirectToPage("Done") : RedirectToPage("Business");
    }

    private async Task LoadOptionsAsync(Guid accountId, CancellationToken ct)
    {
        AvailableClients = await db.Clients.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        AvailableServices = await db.ServiceCatalogItems.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    private void UpdateSelectedLabels()
    {
        var client = AvailableClients.FirstOrDefault(x => x.Id == SelectedClientId);
        ClientName = client?.Name;

        var service = AvailableServices.FirstOrDefault(x => x.Id == SelectedServiceId);
        ServiceName = service?.Name;
        ServicePrice = service?.StandardPrice ?? 0;
    }
}