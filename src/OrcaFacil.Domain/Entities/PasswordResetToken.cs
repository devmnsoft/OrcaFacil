using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public sealed class PasswordResetToken : Entity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string RequestedCorrelationId { get; set; } = string.Empty;
    public string? RequestedIpHash { get; set; }
    public string? UserAgentHash { get; set; }
    public string CreatedBy { get; set; } = "password-recovery";

    public bool IsAvailable(DateTime utcNow) => !IsDeleted && UsedAt is null && RevokedAt is null && ExpiresAt > utcNow;
    public void Use(DateTime utcNow) { if (!IsAvailable(utcNow)) throw new InvalidOperationException("Token indisponível."); UsedAt = utcNow; Touch(); }
    public void Revoke(DateTime utcNow) { if (UsedAt is null && RevokedAt is null) { RevokedAt = utcNow; Touch(); } }
}
