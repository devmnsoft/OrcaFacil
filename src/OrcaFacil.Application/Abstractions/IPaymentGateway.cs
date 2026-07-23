namespace OrcaFacil.Application.Abstractions;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> CreatePixPaymentAsync(PaymentGatewayRequest request, CancellationToken ct = default);
    Task<PaymentGatewayResult> CreateBoletoPaymentAsync(PaymentGatewayRequest request, CancellationToken ct = default);
    Task<PaymentGatewayResult> CreateSubscriptionAsync(PaymentGatewayRequest request, CancellationToken ct = default);
    Task<PaymentGatewayStatus> GetPaymentStatusAsync(string externalPaymentId, CancellationToken ct = default);
    Task<PaymentGatewayWebhookResult> HandleWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);
}

public record PaymentGatewayRequest(string PayerEmail, string DocumentType, string DocumentNumber, decimal Amount, string Description, string ExternalReference, string IdempotencyKey);
public record PaymentGatewayResult(bool Succeeded, string? ExternalPaymentId, string Status, string? PixQrCode = null, string? PixQrCodeBase64 = null, string? PixTicketUrl = null, string? BoletoUrl = null, string? BoletoBarcode = null, string? RawResponseJson = null, string? Error = null);
public record PaymentGatewayStatus(string ExternalPaymentId, string Status, string? RawResponseJson = null);
public record PaymentGatewayWebhookResult(string EventKey, string? ExternalPaymentId, string Status, bool Processed);
