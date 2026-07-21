using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class SystemLog : Entity
{
    public string Level { get; set; } = "Info";
    public string Type { get; set; } = "Application";
    public string Message { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? MetadataJson { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStack { get; set; }
    public string? Environment { get; set; }
    public string? Url { get; set; }
}
