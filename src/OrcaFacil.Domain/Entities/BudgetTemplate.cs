using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class BudgetTemplate : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string Profession { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ConditionsText { get; set; }
    public string? WarrantyText { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Discount { get; set; }
    public bool IsSystemTemplate { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public List<BudgetTemplateItem> Items { get; set; } = [];
}
