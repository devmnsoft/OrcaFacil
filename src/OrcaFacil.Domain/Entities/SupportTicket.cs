using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class SupportTicket : Entity
{
    public Guid AccountId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? PortalUserId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? QueueId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? PriorityId { get; set; }
    public Guid? SlaPolicyId { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public SupportTicketCategory Category { get; set; }
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = "Internal";
    public string Impact { get; set; } = "SingleUser";
    public string Urgency { get; set; } = "Normal";
    public string? RelatedPage { get; set; }
    public string? CorrelationId { get; set; }
    public string? BrowserInfo { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? FirstResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
}

public sealed class UserFeedback : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string PageUrl { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? BrowserInfo { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class KnowledgeBaseArticle : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Audience { get; set; } = "All";
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class ReleaseNote : Entity
{
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ReleasedAt { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

public sealed class SupportTicketMessage : Entity
{
    public Guid TicketId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsAdminReply { get; set; }
    public bool IsInternal { get; set; }
    public SupportMessageType Type { get; set; } = SupportMessageType.PublicReply;
}
