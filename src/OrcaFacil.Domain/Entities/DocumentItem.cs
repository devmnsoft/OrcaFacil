using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class DocumentItem : Entity
{
    public Guid DocumentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; private set; }

    public decimal CalculateTotal() => Total = Math.Max(0, Quantity * UnitPrice - Discount);
}
