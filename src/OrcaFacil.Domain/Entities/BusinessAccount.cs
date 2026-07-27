using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class BusinessAccount : Entity
{
    public string DisplayName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public PersonType PersonType { get; set; }
    public BrazilianDocumentType? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public AccountStatus Status { get; private set; } = AccountStatus.Active;
    public string CurrentPlanCode { get; set; } = "FREE";
    public DateTime? ActivatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAt { get; private set; }
    public DateTime? BlockedAt { get; private set; }
    public string? BlockReason { get; private set; }

    public void Activate() { Status = AccountStatus.Active; ActivatedAt = DateTime.UtcNow; DeactivatedAt = null; BlockedAt = null; BlockReason = null; Touch(); }
    public void Deactivate() { Status = AccountStatus.Inactive; DeactivatedAt = DateTime.UtcNow; Touch(); }
    public void Block(string reason) { if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A justificativa é obrigatória.", nameof(reason)); Status = AccountStatus.Blocked; BlockedAt = DateTime.UtcNow; BlockReason = reason.Trim(); Touch(); }
    public void Close() { Status = AccountStatus.Closed; Touch(); }
}
