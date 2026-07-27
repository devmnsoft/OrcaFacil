using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class BudgetTemplate : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string Profession { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemTemplate { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public List<BudgetTemplateItem> Items { get; set; } = [];
}
