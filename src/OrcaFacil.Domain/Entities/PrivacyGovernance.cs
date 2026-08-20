using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum PrivacyConsentType { TermsOfUse, PrivacyPolicy, EmailCommunication, WhatsAppCommunication, AccountOperation }
public enum PrivacyRequestType { Access, Correction, Export, Anonymization, Deletion, ConsentRevocation }
public enum PrivacyRequestStatus { Open, InReview, WaitingConfirmation, Completed, Rejected, Canceled }
public enum RetentionAction { Keep, Archive, Anonymize, SoftDelete }

public sealed class PrivacyConsent : Entity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public PrivacyConsentType ConsentType { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public sealed class DataExportJob : Entity
{
    public Guid AccountId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Format { get; set; } = "json";
    public string Status { get; set; } = "Completed";
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed class DataRetentionPolicy : Entity
{
    public Guid AccountId { get; set; }
    public string DataType { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public RetentionAction Action { get; set; }
    public bool IsActive { get; set; }
}

public sealed class DataRetentionRun : Entity
{
    public Guid AccountId { get; set; }
    public Guid PolicyId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public bool IsSimulation { get; set; }
    public int MatchedRecords { get; set; }
    public int AffectedRecords { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class SensitiveDataAccessLog : Entity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string AccessType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
}

public sealed class SecurityEvent : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
}

public sealed class SessionRecord : Entity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string SessionHash { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}

public sealed class PublicTokenAccessLog : Entity
{
    public Guid? AccountId { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime AccessedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
}

public sealed class AccountSecuritySetting : Entity
{
    public Guid AccountId { get; set; }
    public int SessionExpirationMinutes { get; set; } = 480;
    public int MinimumPasswordLength { get; set; } = 12;
    public bool RequirePasswordChange { get; set; }
}

public sealed class AuditExportJob : Entity
{
    public Guid AccountId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
