using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum RelationshipStatus { New, Active, Recurring, Inactive, Attention, AtRisk, Delinquent, Strategic, Closed }
public enum CrmInteractionType { Call, WhatsApp, Email, Meeting, Visit, Support, AfterSales, Collection, Negotiation, Renewal, Other }
public enum CampaignChannel { WhatsApp, Email, InternalNotification }
public enum CampaignStatus { Draft, Scheduled, Sending, Sent, PartiallyFailed, Canceled, Archived }
public enum RetentionRiskLevel { Low, Medium, High, Critical }
public enum OpportunityStatus { Open, Converted, Discarded }

public sealed class ClientRelationshipProfile : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public RelationshipStatus Status { get; set; } = RelationshipStatus.New;
    public string StatusReason { get; set; } = "Cliente ainda sem histórico de relacionamento.";
    public string CommercialTemperature { get; set; } = "Cold";
    public DateTime? LastInteractionAt { get; set; }
    public DateTime? NextActionAt { get; set; }
    public Guid? CommercialOwnerUserId { get; set; }
    public Guid? SuccessOwnerUserId { get; set; }
    public string? Source { get; set; }
}

public sealed class ClientInteraction : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public CrmInteractionType InteractionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime InteractionDate { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string? Outcome { get; set; }
    public bool RestrictedVisibility { get; set; }
}

public sealed class ClientHealthScore : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public int Score { get; set; }
    public string Classification { get; set; } = string.Empty;
    public string ExplanationJson { get; set; } = "[]";
    public DateTime CalculatedAt { get; set; }
}

public sealed class CommunicationOptOut : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public CampaignChannel? Channel { get; set; }
    public bool CommercialCommunications { get; set; } = true;
    public string Reason { get; set; } = string.Empty;
    public DateTime OptedOutAt { get; set; }
    public Guid? RegisteredByUserId { get; set; }
}

public sealed class NpsResponse : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public Guid SurveyId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime AnsweredAt { get; set; }
}

public sealed class RetentionRiskEvent : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public string FactorCode { get; set; } = string.Empty;
    public RetentionRiskLevel Level { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public sealed class CrmOpportunity : Entity
{
    public Guid AccountId { get; set; }
    public Guid ClientId { get; set; }
    public string Kind { get; set; } = "Upsell";
    public string Reason { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public DateTime NextActionAt { get; set; }
    public OpportunityStatus Status { get; set; }
    public string? DiscardReason { get; set; }
    public Guid? ConvertedDocumentId { get; set; }
}
