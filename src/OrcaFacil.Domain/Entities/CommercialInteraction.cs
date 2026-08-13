using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class CommercialInteraction : Entity
{
    public Guid AccountId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? DocumentId { get; set; }
    public CommercialInteractionType Type { get; set; }
    public CommercialInteractionChannel Channel { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime? NextFollowUpAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>A reusable, account-scoped commercial message. System defaults are represented
/// by rows without an account and can never be edited by customers.</summary>
public sealed class CommercialMessageTemplate : Entity
{
    public Guid? AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = "General";
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
