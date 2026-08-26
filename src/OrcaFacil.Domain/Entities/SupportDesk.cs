using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class SupportQueue : Entity
{
    public Guid? AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Level { get; set; } = "N1";
    public bool IsGlobal { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SupportQueueMember : Entity
{
    public Guid UserId { get; set; }
    public Guid QueueId { get; set; }
    public string Level { get; set; } = "N1";
    public bool IsLeader { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MaxOpenTickets { get; set; }
}

public sealed class SupportSlaPolicy : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? PlanId { get; set; }
    public Guid? PriorityId { get; set; }
    public Guid? QueueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool BusinessHoursOnly { get; set; }
    public string BusinessDaysJson { get; set; } = "[1,2,3,4,5]";
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SupportTicketEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class SupportTicketSlaEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid TicketId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public sealed class SupportTicketEscalation : Entity
{
    public Guid AccountId { get; set; }
    public Guid TicketId { get; set; }
    public Guid FromQueueId { get; set; }
    public Guid ToQueueId { get; set; }
    public Guid EscalatedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class SupportCsatSurvey : Entity
{
    public Guid AccountId { get; set; }
    public Guid TicketId { get; set; }
    public Guid RequesterUserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public sealed class SupportCsatResponse : Entity
{
    public Guid AccountId { get; set; }
    public Guid SurveyId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool WasResolved { get; set; }
    public bool TimelinessAdequate { get; set; }
}

public sealed class SupportIncident : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "SEV4";
    public SupportIncidentStatus Status { get; set; } = SupportIncidentStatus.Investigating;
    public DateTime StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public bool IsPublicApproved { get; set; }
    public string? ResolutionSummary { get; set; }
}

public sealed class SupportIncidentImpactedAccount : Entity
{
    public Guid IncidentId { get; set; }
    public Guid AccountId { get; set; }
    public bool ShowPublicMessage { get; set; }
}

public sealed class SupportProblemRecord : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportProblemStatus Status { get; set; } = SupportProblemStatus.Open;
    public string? RootCause { get; set; }
    public string? Workaround { get; set; }
    public string? Resolution { get; set; }
    public bool IsPublicApproved { get; set; }
}

public sealed class SupportKnowledgeArticle : Entity
{
    public Guid? AccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Visibility { get; set; } = "Internal";
    public bool IsPublished { get; set; }
}

public sealed class SupportMacro : Entity
{
    public Guid? AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Visibility { get; set; } = "Internal";
    public bool IsActive { get; set; } = true;
}

public sealed class SupportShiftSchedule : Entity
{
    public Guid UserId { get; set; }
    public Guid QueueId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsOnCall { get; set; }
}
