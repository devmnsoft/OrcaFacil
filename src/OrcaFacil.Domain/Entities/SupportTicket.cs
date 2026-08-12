using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class SupportTicket : Entity
{
    public Guid AccountId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public SupportTicketCategory Category { get; set; }
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public sealed class SupportTicketMessage : Entity
{
    public Guid TicketId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsAdminReply { get; set; }
    public bool IsInternal { get; set; }
}
