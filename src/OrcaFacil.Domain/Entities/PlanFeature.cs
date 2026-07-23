using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class PlanFeature : Entity
{
    public string PlanCode { get; set; } = "Free";
    public string FeatureCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int? LimitValue { get; set; }
}
