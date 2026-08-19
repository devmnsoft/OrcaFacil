using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class DocumentRevision : Entity
{
    public Guid AccountId { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public DocumentRevisionStatus Status { get; set; } = DocumentRevisionStatus.Draft;
    public Guid CreatedByUserId { get; set; }
    public DateTime? SentAt { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public string ProtectedSnapshot { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "essential";
    public string BrandingSnapshot { get; set; } = "{}";
    public decimal Total { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool IsCurrent { get; set; }
    public uint Version { get; set; }
}

public sealed class PublicDocumentAccess : Entity
{
    public Guid AccountId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentRevisionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public int ViewCount { get; set; }
    public PublicAccessStatus Status { get; set; } = PublicAccessStatus.Active;
    public Guid CreatedByUserId { get; set; }
    public uint Version { get; set; }
}

public sealed class PublicDocumentDecision : Entity
{
    public Guid AccountId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentRevisionId { get; set; }
    public PublicDocumentDecisionType Decision { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerContact { get; set; }
    public string? ReasonCode { get; set; }
    public string? Comment { get; set; }
    public DateTime? DesiredDate { get; set; }
    public bool AcceptedTerms { get; set; }
    public string IpHash { get; set; } = string.Empty;
    public string UserAgentHash { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class CommercialFollowUp : Entity
{
    public Guid AccountId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? DocumentRevisionId { get; set; }
    public FollowUpChannel Channel { get; set; }
    public FollowUpResult Result { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Note { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class WorkOrder : Entity
{
    public Guid AccountId { get; set; }
    public Guid? ContractId { get; set; }
    public DateOnly? ServiceCompetence { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public Guid? SourceRevisionId { get; set; }
    public Guid ClientId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string AddressSnapshot { get; set; } = "{}";
    public string ClientSnapshot { get; set; } = "{}";
    public string ItemsSnapshot { get; set; } = "[]";
    public decimal TotalSnapshot { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public bool PaymentReceived { get; set; }
    public string? PaymentMethod { get; set; }
    public uint Version { get; set; }
}

public sealed class WorkOrderChecklistItem : Entity
{
    public Guid AccountId { get; set; }
    public Guid WorkOrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool IsRequired { get; set; } = true;
    public int Position { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public string? CompletionNote { get; set; }
}

public sealed class ManualPayment : Entity
{
    public Guid AccountId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid ClientId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
    public Guid RegisteredByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public FinancialRecordStatus Status { get; set; } = FinancialRecordStatus.Active;
    public DateTime? ReversedAt { get; set; }
    public Guid? ReversedByUserId { get; set; }
    public string? ReversalReason { get; set; }
}

public enum FinancialEntryStatus { Pending, PartiallyPaid, Paid, Overdue, Canceled }
public enum FinancialEntryOrigin { Budget, WorkOrder, Contract, Manual }

/// <summary>A tenant-owned amount expected from a customer. Payments remain immutable history.</summary>
public sealed class FinancialEntry : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? ContractPaymentId { get; set; }
    public FinancialEntryOrigin Origin { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public FinancialEntryStatus Status { get; set; } = FinancialEntryStatus.Pending;
    public DateTime? CanceledAt { get; set; }
    public Guid? CanceledByUserId { get; set; }
    public string? CancellationReason { get; set; }
}

public sealed class Receipt : Entity
{
    public Guid AccountId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? LegacyDocumentId { get; set; }
    public Guid ClientId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string IssuerSnapshot { get; set; } = "{}";
    public string ClientSnapshot { get; set; } = "{}";
    public string ServiceSnapshot { get; set; } = "[]";
    public decimal Amount { get; set; }
    public string AmountInWords { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public string FiscalNotice { get; set; } = "Recibo não substitui nota fiscal.";
    public ReceiptOriginType OriginType { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public string? PdfStorageKey { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? LastSharedAt { get; set; }
}

/// <summary>Serializes receipt numbering inside one tenant and calendar year.</summary>
public sealed class ReceiptSequence : Entity
{
    public Guid AccountId { get; set; }
    public int Year { get; set; }
    public long CurrentNumber { get; set; }
    public string Prefix { get; set; } = "REC";
}
