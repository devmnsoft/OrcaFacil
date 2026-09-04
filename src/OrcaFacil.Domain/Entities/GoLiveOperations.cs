using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum GoLiveStatus { NotStarted, InProgress, Blocked, ReadyForPilot, ReadyForProduction, Live, Paused, RolledBack }

public sealed class GoLiveChecklistItem : Entity
{
    public Guid AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public bool IsAutomatic { get; set; }
    public bool IsCompleted { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResponsibleName { get; set; }
    public string? Observation { get; set; }
}

public sealed class GoLiveAccountState : Entity
{
    public Guid AccountId { get; set; }
    public GoLiveStatus Status { get; set; }
    public DateTime? PilotStartedAt { get; set; }
    public DateTime? LiveAt { get; set; }
}

public sealed class TrainingProgress : Entity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string LessonCode { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public bool UserConfirmed { get; set; }
}

public sealed class CriticalRouteEvent : Entity
{
    public Guid? AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string Route { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMilliseconds { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ErrorFingerprint { get; set; }
    public string? SanitizedError { get; set; }
}

public sealed class AssistedOperationAction : Entity
{
    public Guid AccountId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
