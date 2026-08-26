using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class OmnichannelChannel : Entity
{
    public Guid? AccountId { get; set; }
    public OmnichannelChannelType Type { get; set; }
    public OmnichannelChannelStatus Status { get; set; } = OmnichannelChannelStatus.NotConfigured;
    public string Name { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
    public string? LastFailure { get; set; }
}

public sealed class OmnichannelConversation : Entity
{
    public Guid AccountId { get; set; }
    public Guid ChannelId { get; set; }
    public OmnichannelConversationStatus Status { get; set; } = OmnichannelConversationStatus.New;
    public Guid? ClientId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? TicketId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? QueueId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public DateTime? FirstResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public sealed class OmnichannelParticipant : Entity
{
    public Guid AccountId { get; set; }
    public Guid ConversationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class OmnichannelMessage : Entity
{
    public Guid AccountId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid ChannelId { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public Guid? SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public OmnichannelMessageType Type { get; set; }
    public OmnichannelMessageStatus Status { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ExternalMessageId { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? FailedAt { get; set; }
}

public sealed class OmnichannelDeliveryLog : Entity
{
    public Guid AccountId { get; set; }
    public Guid MessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? SanitizedError { get; set; }
}

public sealed class OmnichannelWebChatSession : Entity
{
    public Guid AccountId { get; set; }
    public Guid ConversationId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string VisitorName { get; set; } = string.Empty;
    public string VisitorEmail { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool ConsentAccepted { get; set; }
}

public sealed class OmnichannelInboundEmailAccount : Entity
{
    public Guid AccountId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Mode { get; set; } = "Manual";
    public bool HasProtectedCredential { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class OmnichannelOptOutEvent : Entity
{
    public Guid AccountId { get; set; }
    public string IdentityHash { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Scope { get; set; } = "Commercial";
    public DateTime OptedOutAt { get; set; } = DateTime.UtcNow;
}

public sealed class OmnichannelSlaEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid ConversationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public sealed class OmnichannelCsatResponse : Entity
{
    public Guid AccountId { get; set; }
    public Guid ConversationId { get; set; }
    public string RequestTokenHash { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
