using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public sealed class CommercialLead : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Segment { get; set; }
    public int? MonthlyBudgetVolume { get; set; }
    public string? Message { get; set; }
    public bool ConsentAccepted { get; set; }
    public string SourcePage { get; set; } = "/";
    public CommercialLeadStatus Status { get; set; } = CommercialLeadStatus.New;
    public Guid? ConvertedAccountId { get; set; }
    public Guid? ConvertedClientId { get; set; }
    public string? InternalNotes { get; set; }
    public string? DiscardReason { get; set; }
}
