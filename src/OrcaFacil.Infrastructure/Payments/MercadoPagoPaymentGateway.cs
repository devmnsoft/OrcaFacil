using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Payments;

namespace OrcaFacil.Infrastructure.Payments;

public class MercadoPagoPaymentGateway : IPaymentGateway
{
    private readonly MercadoPagoOptions _options;
    public MercadoPagoPaymentGateway(IOptions<MercadoPagoOptions> options) => _options = options.Value;
    public Task<PaymentGatewayResult> CreatePixPaymentAsync(PaymentGatewayRequest r, CancellationToken ct = default) => UnavailableAsync();
    public Task<PaymentGatewayResult> CreateBoletoPaymentAsync(PaymentGatewayRequest r, CancellationToken ct = default) => UnavailableAsync();
    public Task<PaymentGatewayResult> CreateSubscriptionAsync(PaymentGatewayRequest r, CancellationToken ct = default) => UnavailableAsync();
    public Task<PaymentGatewayStatus> GetPaymentStatusAsync(string externalPaymentId, CancellationToken ct = default) => Task.FromResult(new PaymentGatewayStatus(externalPaymentId, "pending", "{}"));
    public Task<PaymentGatewayWebhookResult> HandleWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.WebhookSecret))
            return Task.FromResult(new PaymentGatewayWebhookResult("unvalidated", null, "provider_not_configured", false));
        if (!headers.TryGetValue("x-signature", out var signature) || string.IsNullOrWhiteSpace(signature))
            return Task.FromResult(new PaymentGatewayWebhookResult("unvalidated", null, "invalid_signature", false));
        // Full provider validation must be introduced together with the real HTTP client. Never trust payload-only events.
        return Task.FromResult(new PaymentGatewayWebhookResult("unvalidated", null, "validation_not_implemented", false));
    }
    private Task<PaymentGatewayResult> UnavailableAsync()
    {
        var code = !_options.Enabled || string.IsNullOrWhiteSpace(_options.AccessToken)
            ? "provider_not_configured" : "provider_integration_unavailable";
        return Task.FromResult(new PaymentGatewayResult(false, null, code,
            Error: "Checkout indisponível no momento. Fale com a MNSOFT."));
    }
}
