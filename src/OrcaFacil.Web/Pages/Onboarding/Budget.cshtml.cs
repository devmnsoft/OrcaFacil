using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
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
    public string? ClientName { get; private set; }
    public string? ServiceName { get; private set; }
    public decimal ServicePrice { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        State = (await onboarding.GetAsync(ct)).Value!;
        if (currentAccount.AccountId is Guid accountId)
        {
            var client = await db.Clients.AsNoTracking()
                .Where(x => x.AccountId == accountId && !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
            ClientName = client?.Name;

            var service = await db.ServiceCatalogItems.AsNoTracking()
                .Where(x => x.AccountId == accountId && !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
            ServiceName = service?.Name;
            ServicePrice = service?.StandardPrice ?? 0;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (currentAccount.AccountId is not Guid accountId) return Forbid();

        await onboarding.StartBudgetAsync(ct);

        var client = await db.Clients.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var service = await db.ServiceCatalogItems.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var seedServices = service is not null ? new[] { service.Id } : Array.Empty<Guid>();
        var idempotencyKey = $"onboarding-{accountId:N}";

        var openResult = await wizard.OpenAsync(
            currentAccount.UserId,
            accountId,
            documentId: null,
            clientId: client?.Id,
            ct: ct,
            serviceIds: seedServices,
            templateId: null,
            idempotencyKey: idempotencyKey);

        if (openResult.Succeeded && openResult.Draft is not null)
        {
            return RedirectToPage("/Documents/CreateBudget", new { id = openResult.Draft.DocumentId });
        }

        return RedirectToPage("/Documents/New");
    }

    public async Task<IActionResult> OnPostDoneAsync(CancellationToken ct)
    {
        var r = await onboarding.CompleteAsync(ct);
        return r.Succeeded ? RedirectToPage("Done") : RedirectToPage("Business");
    }
}