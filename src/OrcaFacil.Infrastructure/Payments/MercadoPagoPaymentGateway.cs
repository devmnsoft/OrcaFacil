using System.Text.Json;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Payments;

namespace OrcaFacil.Infrastructure.Payments;

public class MercadoPagoPaymentGateway : IPaymentGateway
{
    private readonly MercadoPagoOptions _options;
    public MercadoPagoPaymentGateway(IOptions<MercadoPagoOptions> options) => _options = options.Value;
    public Task<PaymentGatewayResult> CreatePixPaymentAsync(PaymentGatewayRequest r, CancellationToken ct = default) => CreatePreparedAsync(r, "pix");
    public Task<PaymentGatewayResult> CreateBoletoPaymentAsync(PaymentGatewayRequest r, CancellationToken ct = default) => CreatePreparedAsync(r, "bolbradesco");
    public Task<PaymentGatewayResult> CreateSubscriptionAsync(PaymentGatewayRequest r, CancellationToken ct = default) => CreatePreparedAsync(r, "subscription");
    public Task<PaymentGatewayStatus> GetPaymentStatusAsync(string externalPaymentId, CancellationToken ct = default) => Task.FromResult(new PaymentGatewayStatus(externalPaymentId, "pending", "{}"));
    public Task<PaymentGatewayWebhookResult> HandleWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        using var json = string.IsNullOrWhiteSpace(rawBody) ? JsonDocument.Parse("{}") : JsonDocument.Parse(rawBody);
        var id = json.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var pid) ? pid.ToString() : null;
        var key = json.RootElement.TryGetProperty("id", out var eid) ? eid.ToString() : $"mp-{id}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return Task.FromResult(new PaymentGatewayWebhookResult(key, id, "pending", true));
    }
    private Task<PaymentGatewayResult> CreatePreparedAsync(PaymentGatewayRequest r, string method)
    {
        if (!_options.Enabled) return Task.FromResult(new PaymentGatewayResult(false, null, "disabled", Error: "Mercado Pago desativado."));
        var raw = JsonSerializer.Serialize(new { method, r.ExternalReference, r.IdempotencyKey, sandbox = _options.Environment });
        return Task.FromResult(new PaymentGatewayResult(true, $"prepared-{Guid.NewGuid():N}", "pending", RawResponseJson: raw));
    }
}
