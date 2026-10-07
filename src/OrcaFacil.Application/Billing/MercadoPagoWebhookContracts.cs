namespace OrcaFacil.Application.Billing;

public sealed record WebhookProcessingResult(
    bool Succeeded,
    bool Duplicate,
    string EventKey,
    string? Message = null,
    string? Status = null);

public interface IMercadoPagoWebhookProcessor
{
    Task<WebhookProcessingResult> ProcessAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        string? correlationId = null,
        CancellationToken ct = default);
}
