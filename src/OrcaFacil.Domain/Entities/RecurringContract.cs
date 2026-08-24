using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum ContractStatus { Draft, PendingApproval, Active, Paused, Suspended, PendingRenewal, Renewed, Expired, Canceled, Terminated, Finished }
public enum RecurrencePeriod { Monthly, Bimonthly, Quarterly, Semiannual, Annual, Custom }
public enum RecurringPaymentStatus { Forecast, Pending, Paid, Overdue, Canceled, Reversed }

public sealed class RecurringContract : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public decimal RecurringAmount { get; set; }
    public RecurrencePeriod Periodicity { get; set; } = RecurrencePeriod.Monthly;
    public int? CustomPeriodMonths { get; set; }
    public int DueDay { get; set; } = 10;
    public DateOnly? NextBillingDate { get; set; }
    public DateOnly? NextServiceDate { get; set; }
    public string? CommercialTerms { get; set; }
    public string? InternalNotes { get; set; }
    public string? CustomerNotes { get; set; }
    public bool AutoRenew { get; set; }
    public int RenewalNoticeDays { get; set; } = 30;
    public int? ResponseSlaHours { get; set; }
    public int? ExecutionSlaHours { get; set; }
    public string Priority { get; set; } = "Normal";
    public bool SlaUsesBusinessDays { get; set; } = true;
    public string? ServiceHours { get; set; }
    public string? SlaNotes { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? RenewedFromContractId { get; set; }
    public List<ContractItem> Items { get; set; } = [];
}

public enum ContractSlaPriority { Low, Normal, High, Critical }
public enum WarrantyStatus { Active, Expired, Voided, Used, Canceled }
public enum PreventiveFrequency { Weekly, Monthly, Bimonthly, Quarterly, Semiannual, Annual, Custom }
public enum ContractAdjustmentStatus { Draft, PendingApproval, Approved, Applied, Rejected, Canceled }

public sealed class ContractSlaPolicy : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ContractSlaPriority Priority { get; set; } = ContractSlaPriority.Normal;
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
    public bool BusinessHoursOnly { get; set; }
    public string BusinessDaysJson { get; set; } = "[1,2,3,4,5]";
    public TimeOnly StartTime { get; set; } = new(8, 0);
    public TimeOnly EndTime { get; set; } = new(18, 0);
    public bool IsActive { get; set; } = true;
}

public sealed class ContractSlaEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? SupportTicketId { get; set; }
    public Guid SlaPolicyId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Details { get; set; }
}

public sealed class ServiceLevelBreach : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid SlaPolicyId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string BreachType { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class ContractWarrantyTerm : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public string Coverage { get; set; } = string.Empty;
    public string? Conditions { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public WarrantyStatus Status { get; set; } = WarrantyStatus.Active;
    public string? CancellationReason { get; set; }
}

public sealed class ContractPreventiveSchedule : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PreventiveFrequency Frequency { get; set; }
    public int Interval { get; set; } = 1;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextRunDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ContractRecurrenceRun : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid? PreventiveScheduleId { get; set; }
    public string RunType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Started";
    public Guid? GeneratedEntityId { get; set; }
    public string? ErrorSummary { get; set; }
}

public sealed class ContractUsageAllowance : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public string UsageType { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AllowanceQuantity { get; set; }
    public decimal UsedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class ContractAmendment : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public string AmendmentNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public string PreviousSnapshotJson { get; set; } = "{}";
    public string NewSnapshotJson { get; set; } = "{}";
    public Guid? ApprovedByUserId { get; set; }
}

public sealed class ContractAdjustment : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public decimal? Percent { get; set; }
    public decimal? Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public decimal OldValue { get; set; }
    public decimal NewValue { get; set; }
    public ContractAdjustmentStatus Status { get; set; } = ContractAdjustmentStatus.Draft;
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
}

public sealed class ContractRenewalEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? ApprovedByUserId { get; set; }
    public string? Reason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class ContractHealthSnapshot : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public int Score { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string PositiveFactorsJson { get; set; } = "[]";
    public string RiskFactorsJson { get; set; } = "[]";
    public string NextAction { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ContractItem : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid? ServiceCatalogItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Checklist { get; set; }
}

public sealed class ContractPayment : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid ClientId { get; set; }
    public DateOnly Competence { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public RecurringPaymentStatus Status { get; set; } = RecurringPaymentStatus.Forecast;
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public Guid? ManualPaymentId { get; set; }
}

public sealed class ContractEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid ContractId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedUrl { get; set; }
}
