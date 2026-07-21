using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class SystemError : Entity
{
    public string Message { get; set; } = string.Empty;
    public string? Stack { get; set; }
    public string? Code { get; set; }
    public string Severity { get; set; } = "Error";
    public Guid? UserId { get; set; }
    public string? ContextJson { get; set; }
    public bool Resolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? AdminNote { get; set; }
}
