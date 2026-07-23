using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class BudgetTemplateItem : Entity
{
    public Guid BudgetTemplateId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = "un";
    public int SortOrder { get; set; }
}
