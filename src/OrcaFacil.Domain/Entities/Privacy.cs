using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum LegalDocumentType { TermsOfUse, PrivacyNotice, CookieNotice }
public enum LegalDocumentStatus { Draft, UnderLegalReview, Approved, Published, Superseded }
public enum LegalAcceptanceSource { Registration, LoginReacceptance, AccountSettings, PublicPortal }
public enum CommunicationChannel { Email, WhatsApp }
public enum CommunicationPurpose { ProductNews, Offers, EducationalContent }
public enum DataSubjectRequestType { Confirmation, Access, Correction, SharingInformation, Portability, Deletion, ConsentRevocation, Objection, AccountClosure }
public enum DataSubjectRequestStatus { Received, IdentityVerificationRequired, InReview, Processing, Completed, PartiallyCompleted, Rejected, Cancelled }

public class LegalDocument : Entity
{
    public LegalDocumentType Type { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<LegalDocumentVersion> Versions { get; set; } = [];
}

public class LegalDocumentVersion : Entity
{
    public Guid LegalDocumentId { get; set; }
    public LegalDocument? LegalDocument { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public LegalDocumentStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? EffectiveAt { get; set; }
    public bool RequiresReacceptance { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class LegalAcceptance : Entity
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public Guid LegalDocumentVersionId { get; set; }
    public DateTime AcceptedAt { get; set; }
    public LegalAcceptanceSource AcceptanceSource { get; set; }
    public string IpHash { get; set; } = string.Empty;
    public string UserAgentHash { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
}

public class CommunicationConsent : Entity
{
    public Guid UserId { get; set; }
    public CommunicationChannel Channel { get; set; }
    public CommunicationPurpose Purpose { get; set; }
    public bool Granted { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string LegalTextVersion { get; set; } = string.Empty;
    public string IpHash { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
}

public class DataSubjectRequest : Entity
{
    public Guid RequesterUserId { get; set; }
    public Guid AccountId { get; set; }
    public DataSubjectRequestType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Received;
    public DateTime RequestedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? DeliveryFileId { get; set; }
}

public class PrivacyVendor : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string DataCategories { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PrivacyUrl { get; set; } = string.Empty;
    public string ContractStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastReviewedAt { get; set; }
}

public class PrivacyProcessingActivity : Entity
{
    public string Name { get; set; } = string.Empty;
    public string DataCategories { get; set; } = string.Empty;
    public string DataSubjects { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string Systems { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;
    public string? InternationalTransfer { get; set; }
    public string RetentionRule { get; set; } = string.Empty;
    public string SecurityControls { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
