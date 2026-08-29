using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum PriceTableScope { Account, Customer, Segment, Contract, Campaign, Partner }
public enum CommercialApprovalStatus { Pending, Approved, Rejected }

public sealed class ServicePriceTable : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PriceTableScope Scope { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SegmentId { get; set; }
    public Guid? ContractId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; set; } = 1;
}

public sealed class ServicePriceTableItem : Entity
{
    public Guid AccountId { get; set; }
    public Guid ServicePriceTableId { get; set; }
    public Guid ServiceCatalogItemId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal MinimumPrice { get; set; }
    public decimal? MaximumPrice { get; set; }
    public decimal MinimumMarginPercentage { get; set; }
    public decimal MaximumDiscountPercentage { get; set; }
    public string UnitCode { get; set; } = "service";
}

public sealed class PricingMarginPolicy : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MinimumMarginPercentage { get; set; }
    public decimal TargetMarginPercentage { get; set; }
    public string Scope { get; set; } = "account";
    public Guid? ScopeReferenceId { get; set; }
    public bool RequiresApprovalBelowMinimum { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class PricingDiscountPolicy : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MaximumPercentageWithoutApproval { get; set; }
    public decimal MaximumAmountWithoutApproval { get; set; }
    public Guid ApproverUserId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PricingQuoteSnapshot : Entity
{
    public Guid AccountId { get; set; }
    public Guid QuoteId { get; set; }
    public int Sequence { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public decimal BasePrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal MarginPercentage { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PricingApprovalEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid ApproverUserId { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public CommercialApprovalStatus Status { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}
