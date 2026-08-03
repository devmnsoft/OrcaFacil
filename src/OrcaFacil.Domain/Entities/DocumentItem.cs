using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class DocumentItem : Entity
{
    public Guid DocumentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? ServiceCatalogItemId { get; set; }
    public decimal EstimatedCostSnapshot { get; set; }
    public string? CategorySnapshot { get; set; }
    public int? DurationMinutesSnapshot { get; set; }
    public decimal EstimatedTotalCost => Math.Max(0, Quantity * EstimatedCostSnapshot);
    public decimal EstimatedMargin => CalculateTotal() - EstimatedTotalCost;
    public string Unit { get; set; } = "serviço";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public decimal Total { get; private set; }

    public decimal CalculateTotal() => Total = Math.Max(0, Quantity * UnitPrice - Discount);
}
