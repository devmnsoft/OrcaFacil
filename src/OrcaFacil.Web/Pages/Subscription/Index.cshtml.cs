using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Billing;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Subscription;

[Authorize]
public sealed class IndexModel(
    IPlanExperienceService experience,
    ISubscriptionCheckoutService checkoutService,
    ICurrentAccountService currentAccount,
    ICurrentUserService currentUser,
    OrcaFacilDbContext db) : PageModel
{
    public PlanExperienceViewModel Plan { get; private set; } = default!;
    public BillingCustomerProfile? BillingProfile { get; private set; }
    public IReadOnlyList<InvoiceHistoryRow> Invoices { get; private set; } = [];
    public Payment? ActiveCheckoutPayment { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Plan = await experience.GetAsync(ct);
        BillingProfile = await db.BillingCustomerProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == currentUser.UserId && !x.IsDeleted, ct);

        if (currentAccount.AccountId is { } accountId)
        {
            Invoices = await db.BillingInvoices.AsNoTracking()
                .Where(x => x.AccountId == accountId && !x.IsDeleted)
                .OrderByDescending(x => x.DueAt)
                .Take(12)
                .Select(x => new InvoiceHistoryRow(
                    x.Id,
                    x.InvoiceNumber,
                    x.DueAt,
                    x.Amount,
                    x.PaidAmount,
                    x.Status.ToString(),
                    x.PaidAt))
                .ToListAsync(ct);

            ActiveCheckoutPayment = await db.Payments.AsNoTracking()
                .Where(x => x.AccountId == accountId && x.Status == PaymentStatus.Pending && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }
    }

    public async Task<IActionResult> OnPostCheckoutAsync(string planCode, string billingCycle, CancellationToken ct)
    {
        if (currentAccount.AccountId is not { } accountId)
        {
            TempData["Error"] = "Selecione uma conta para contratar ou regularizar o plano.";
            return RedirectToPage();
        }

        var profile = await db.BillingCustomerProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == currentUser.UserId && !x.IsDeleted, ct);

        var docType = profile?.DocumentType == BrazilianDocumentType.CNPJ ? "CNPJ" : "CPF";
        var docNumber = !string.IsNullOrWhiteSpace(profile?.DocumentNumber)
            ? profile.DocumentNumber
            : "00000000000";

        var payerEmail = !string.IsNullOrWhiteSpace(profile?.Email)
            ? profile.Email
            : (!string.IsNullOrWhiteSpace(currentUser.Email) ? currentUser.Email : "contato@orcafacil.com.br");

        var cycle = billingCycle?.Trim().ToLowerInvariant() == "annual" ? "annual" : "monthly";
        var idempotencyKey = $"checkout_{accountId:N}_{planCode}_{cycle}_{DateTime.UtcNow:yyyyMMddHH}";

        var request = new CheckoutRequest(
            accountId,
            planCode,
            cycle,
            payerEmail,
            docType,
            docNumber,
            idempotencyKey);

        var result = await checkoutService.CreateAsync(request, ct);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToPage();
        }

        if (result.RedirectUri != null)
        {
            return Redirect(result.RedirectUri.ToString());
        }

        TempData["Success"] = $"Solicitação de assinatura iniciada com sucesso ({result.Code}). Conclua o pagamento pelo link de checkout.";
        return RedirectToPage();
    }
}

public sealed record InvoiceHistoryRow(
    Guid Id,
    string Number,
    DateTime DueAt,
    decimal Amount,
    decimal PaidAmount,
    string Status,
    DateTime? PaidAt);
