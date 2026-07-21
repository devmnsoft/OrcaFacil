using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.ValueObjects;

namespace OrcaFacil.Domain.Entities;

public class PublicQuote : Entity
{
    public string Token { get; set; } = new PublicToken().Value;
    public Guid OwnerUserId { get; set; }
    public Guid DocumentId { get; set; }
    public bool PublicEnabled { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public ClientDecision DecisionStatus { get; set; } = ClientDecision.Pending;
    public string? DecisionNote { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecidedByName { get; set; }
    public string? DecidedByDocument { get; set; }
    public string? DecidedByEmail { get; set; }
    public bool AcceptedTerms { get; set; }
    public string? EvidenceHash { get; set; }
    public string? UserAgent { get; set; }
}
