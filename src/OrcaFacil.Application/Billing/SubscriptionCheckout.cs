using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Plans;

namespace OrcaFacil.Application.Billing;

public interface ISubscriptionCheckoutService
{
    Task<CheckoutResult> CreateAsync(CheckoutRequest request, CancellationToken ct = default);
}

public sealed record CheckoutRequest(
    Guid AccountId,
    string PlanCode,
    string BillingCycle,
    string PayerEmail,
    string DocumentType,
    string DocumentNumber,
    string IdempotencyKey);

public sealed record CheckoutResult(
    bool Succeeded,
    string Code,
    string Message,
    Uri? RedirectUri = null,
    string? ProviderSessionId = null);

public sealed record WebhookValidationResult(bool IsValid, string Code, string? EventKey = null);

public sealed class PaymentProviderOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "MercadoPago";
}

/// <summary>Starts checkout without ever changing subscription state; only a validated webhook may do that.</summary>
public sealed class SubscriptionCheckoutService(IPaymentGateway gateway, IPlanCatalogService catalog) : ISubscriptionCheckoutService
{
    public async Task<CheckoutResult> CreateAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        if (request.AccountId == Guid.Empty || string.IsNullOrWhiteSpace(request.PlanCode))
            return new(false, "invalid_request", "Não foi possível iniciar a contratação. Revise os dados informados.");

        var cycle = request.BillingCycle?.Trim().ToLowerInvariant();
        if (cycle != "monthly" && cycle != "annual")
            return new(false, "invalid_billing_cycle", "Periodicidade de cobrança inválida. Escolha mensal ou anual.");

        var published = await catalog.GetPublishedAsync(ct);
        var plan = published.Plans.SingleOrDefault(x => x.Code.Equals(request.PlanCode, StringComparison.OrdinalIgnoreCase));
        if (plan is null || plan.MonthlyPrice <= 0)
            return new(false, "plan_not_available", "Este plano não está disponível para contratação.");

        var annual = cycle == "annual";
        var amount = annual ? plan.AnnualPrice : plan.MonthlyPrice;
        if (amount <= 0)
            return new(false, "billing_cycle_not_available", "Esta periodicidade não está disponível.");

        var result = await gateway.CreateSubscriptionAsync(new(
            request.PayerEmail,
            request.DocumentType,
            request.DocumentNumber,
            amount,
            $"Plano {plan.Name} - {(annual ? "anual" : "mensal")}",
            request.AccountId.ToString("N"),
            request.IdempotencyKey,
            BillingCycle: cycle), ct);

        if (!result.Succeeded)
            return new(false, result.Status, result.Error ?? "Checkout indisponível no momento. Fale com a MNSOFT.");

        var redirectTarget = result.CheckoutUrl ?? result.PixTicketUrl ?? result.BoletoUrl;
        return new(
            true,
            "checkout_created",
            "Checkout criado pelo provedor.",
            Uri.TryCreate(redirectTarget, UriKind.Absolute, out var uri) ? uri : null,
            result.ExternalPaymentId);
    }
}
