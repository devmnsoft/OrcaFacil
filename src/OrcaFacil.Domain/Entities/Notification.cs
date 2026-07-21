using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public class Notification : Entity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public bool Read { get; set; }
    public Guid? DocumentId { get; set; }
}
