using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class AdminSetting : Entity
{
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public Guid? UpdatedBy { get; set; }
}
