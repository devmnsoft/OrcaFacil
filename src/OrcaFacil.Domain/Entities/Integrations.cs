using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public sealed class IntegrationSetting : Entity
{
    public Guid AccountId { get; set; }
    public string? PublicBaseUrl { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? EmailSignature { get; set; }
    public string? SupportEmail { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUser { get; set; }
    public string? ProtectedSmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = true;
    public bool EmailSendingEnabled { get; set; }
}

public sealed class WebhookEndpoint : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string ProtectedSecret { get; set; } = string.Empty;
    public string EventTypes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastDeliveryAt { get; set; }
}

public enum WebhookDeliveryStatus { Pending, Processing, Delivered, Failed, Canceled }
public sealed class WebhookDelivery : Entity
{
    public Guid AccountId { get; set; }
    public Guid WebhookEndpointId { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public WebhookDeliveryStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public string? LastErrorSummary { get; set; }
}

public sealed class ApiKey : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class DataExport : Entity
{
    public Guid AccountId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string Format { get; set; } = "CSV";
    public int RowCount { get; set; }
    public DateTime CompletedAt { get; set; }
}
