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
