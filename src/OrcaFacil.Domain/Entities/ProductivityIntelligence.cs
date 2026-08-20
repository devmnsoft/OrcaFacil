using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public sealed class RecommendationCard : Entity
{
    public Guid AccountId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? PublicQuoteId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? ReceivableId { get; set; }
    public Guid? ContractId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime? ResolvedAt { get; set; }
}

public sealed class AutomationRule : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string ActionType { get; set; } = "CreateRecommendation";
    public bool IsActive { get; set; } = true;
    public string ConditionsJson { get; set; } = "{}";
    public DateTime? LastRunAt { get; set; }
}

public sealed class AutomationRun : Entity
{
    public Guid AccountId { get; set; }
    public Guid AutomationRuleId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public string ResultSummary { get; set; } = string.Empty;
}

public sealed class ProductivityEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
