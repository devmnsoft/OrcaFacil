using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public sealed class ServiceCatalogItem : Entity
{
    public Guid AccountId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public string UnitCode { get; set; } = "service";
    public decimal StandardPrice { get; set; }
    public decimal EstimatedCost { get; set; }
    public int? SuggestedDurationMinutes { get; set; }
    public string? InternalNotes { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsActive { get; set; } = true;
    public int UseCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public uint Version { get; set; }
    public decimal Margin => StandardPrice - EstimatedCost;
}

public sealed class ServiceCategory : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IconName { get; set; } = "service";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ServicePriceHistory : Entity
{
    public Guid AccountId { get; set; }
    public Guid ServiceCatalogItemId { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal PreviousCost { get; set; }
    public decimal NewCost { get; set; }
    public string? Reason { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
