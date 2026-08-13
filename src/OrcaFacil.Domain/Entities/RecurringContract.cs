using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum ContractStatus { Draft, Active, Paused, PendingRenewal, Expired, Canceled, Finished }
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
