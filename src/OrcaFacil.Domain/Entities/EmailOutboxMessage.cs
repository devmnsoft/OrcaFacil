using OrcaFacil.Domain.Common;
namespace OrcaFacil.Domain.Entities;
public enum EmailOutboxStatus { Pending, Processing, Sent, Failed, Canceled, DeadLetter }
public enum EmailPriority { Critical, High, Normal, Low }
public sealed class EmailOutboxMessage : Entity
{
    public Guid? AccountId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string RecipientHash { get; set; } = string.Empty;
    public string RecipientMasked { get; set; } = string.Empty;
    public string ProtectedRecipient { get; set; } = string.Empty;
    public string? ProtectedPayload { get; set; }
    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAt { get; set; }
    public string? ProcessingInstanceId { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeadLetteredAt { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorSummary { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}
