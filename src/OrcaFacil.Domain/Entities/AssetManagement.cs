using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum AssetStatus { Active, UnderMaintenance, Inactive, Replaced, Disposed, UnderWarranty, WithoutContract }
public enum AssetCriticality { Low, Normal, High, Critical }
public enum MaintenanceFrequency { Daily, Weekly, Fortnightly, Monthly, Bimonthly, Quarterly, Semiannual, Annual, Meter, Counter, Manual }
public enum InspectionStatus { Draft, InProgress, Completed, PendingReview, Approved, Rejected, Cancelled }
public enum NonConformitySeverity { Low, Medium, High, Critical }
public enum NonConformityStatus { Open, UnderAnalysis, ActionPlanCreated, UnderCorrection, Corrected, Validated, Cancelled }
public enum ActionPlanStatus { Open, InProgress, AwaitingValidation, Completed, Overdue, Cancelled }
public enum TechnicalReportStatus { Draft, UnderReview, Approved, Published, Cancelled }

public sealed class AssetCategory : Entity
{
    public Guid? AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsGlobal => AccountId is null;
}

public sealed class AssetModel : Entity
{
    public Guid? AccountId { get; set; }
    public Guid CategoryId { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedUsefulLifeMonths { get; set; }
    public int? SuggestedMaintenanceDays { get; set; }
    public Guid? DefaultChecklistId { get; set; }
    public string? RecommendedDocuments { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CustomerAssetLocation : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public string? Room { get; set; }
    public string? Reference { get; set; }
    public string? LocalContact { get; set; }
    public string? Notes { get; set; }
}

public sealed class CustomerAsset : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? InventoryNumber { get; set; }
    public string? Manufacturer { get; set; }
    public DateOnly? InstalledOn { get; set; }
    public DateOnly? AcquiredOn { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public AssetCriticality Criticality { get; set; } = AssetCriticality.Normal;
    public string? Notes { get; set; }
}

public sealed class CustomerAssetWarranty : Entity
{
    public Guid AccountId { get; set; }
    public Guid AssetId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Supplier { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string? Conditions { get; set; }
    public Guid? DocumentId { get; set; }
}

public sealed class MaintenancePlan : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? ContractId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MaintenanceFrequency Frequency { get; set; }
    public int Interval { get; set; } = 1;
    public string MaintenanceType { get; set; } = "Preventiva";
    public Guid? ChecklistId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class MaintenancePlanAsset : Entity
{
    public Guid AccountId { get; set; }
    public Guid PlanId { get; set; }
    public Guid AssetId { get; set; }
    public DateTime NextDueAt { get; set; }
}

public sealed class MaintenanceGeneratedWorkOrder : Entity
{
    public Guid AccountId { get; set; }
    public Guid PlanId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Error { get; set; }
}

public sealed class InspectionTemplate : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ModelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsPublished { get; set; }
}
public sealed class InspectionTemplateItem : Entity
{
    public Guid? AccountId { get; set; }
    public Guid TemplateId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string AnswerType { get; set; } = "YesNo";
    public bool IsRequired { get; set; }
    public bool IsCritical { get; set; }
    public int DisplayOrder { get; set; }
}
public sealed class AssetInspection : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid AssetId { get; set; }
    public Guid TemplateId { get; set; }
    public Guid TechnicianUserId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public InspectionStatus Status { get; set; } = InspectionStatus.Draft;
    public string? Result { get; set; }
    public string? RejectionReason { get; set; }
}
public sealed class AssetInspectionAnswer : Entity
{
    public Guid AccountId { get; set; }
    public Guid InspectionId { get; set; }
    public Guid TemplateItemId { get; set; }
    public string? Value { get; set; }
    public bool IsNonConforming { get; set; }
}
public sealed class NonConformity : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? InspectionId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string Description { get; set; } = string.Empty;
    public NonConformitySeverity Severity { get; set; }
    public NonConformityStatus Status { get; set; } = NonConformityStatus.Open;
    public Guid? ResponsibleUserId { get; set; }
    public DateTime? DueAt { get; set; }
    public string? CancellationReason { get; set; }
}
public sealed class CorrectiveActionPlan : Entity
{
    public Guid AccountId { get; set; }
    public Guid NonConformityId { get; set; }
    public Guid ResponsibleUserId { get; set; }
    public DateTime DueAt { get; set; }
    public ActionPlanStatus Status { get; set; }
}
public sealed class CorrectiveActionItem : Entity
{
    public Guid AccountId { get; set; }
    public Guid PlanId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid ResponsibleUserId { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? EvidenceFileId { get; set; }
}
public sealed class TechnicalReport : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid AssetId { get; set; }
    public string OriginType { get; set; } = string.Empty;
    public Guid OriginId { get; set; }
    public Guid TechnicalResponsibleUserId { get; set; }
    public string? Conclusion { get; set; }
    public string? Recommendations { get; set; }
    public TechnicalReportStatus Status { get; set; }
    public int Version { get; set; } = 1;
    public DateTime? PublishedAt { get; set; }
}
public sealed class AssetQrCode : Entity
{
    public Guid AccountId { get; set; }
    public Guid AssetId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
public sealed class AssetQrAccessLog : Entity
{
    public Guid AccountId { get; set; }
    public Guid QrCodeId { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public string? IpHash { get; set; }
}
