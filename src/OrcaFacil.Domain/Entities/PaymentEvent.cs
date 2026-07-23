using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class PaymentEvent : Entity
{
    public Guid PaymentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public string? CorrelationId { get; set; }
}
