using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class MercadoPagoWebhookEvent : Entity
{
    public string EventKey { get; set; } = string.Empty;
    public string? ExternalPaymentId { get; set; }
    public string? Topic { get; set; }
    public string RawJson { get; set; } = "{}";
    public bool Processed { get; set; }
    public string? CorrelationId { get; set; }
}
