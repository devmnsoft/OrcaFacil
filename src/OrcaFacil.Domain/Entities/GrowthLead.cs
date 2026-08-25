using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public sealed class GrowthLead : Entity
{
    public Guid TenantOwnerAccountId { get; set; }
    public Guid? AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public string? Segment { get; set; }
    public string? CompanySize { get; set; }
    public string Interest { get; set; } = string.Empty;
    public string? DesiredPlan { get; set; }
    public string? Message { get; set; }
    public string Source { get; set; } = "Direct/Unknown";
    public string Channel { get; set; } = "Direct/Unknown";
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmTerm { get; set; }
    public string? UtmContent { get; set; }
    public string? ReferralCode { get; set; }
    public string? Gclid { get; set; }
    public string? Fbclid { get; set; }
    public string? LandingPage { get; set; }
    public string? Referrer { get; set; }
    public DateTime? ConsentAt { get; set; }
    public string Status { get; set; } = "New";
    public Guid? ConvertedAccountId { get; set; }
    public Guid? LossReasonId { get; set; }
}

public sealed class GrowthLeadEvent : Entity
{
    public Guid LeadId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public Guid? ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
