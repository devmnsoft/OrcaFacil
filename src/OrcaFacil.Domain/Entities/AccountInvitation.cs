using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

/// <summary>
/// Convite de equipe por e-mail: a conta chama uma pessoa para colaborar com um
/// perfil. O token cru (gerado no serviço) nunca é gravado — só o hash SHA-256 —
/// o aceite é único, com expiração, e a revogação expira com histórico
/// preservado (sem DROP/sem reuso).
/// </summary>
public class AccountInvitation : Entity
{
    public Guid AccountId { get; set; }
    public string Email { get; set; } = string.Empty; // normalizado (minúsculas)
    public string FullName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = "ReadOnly";
    public Guid InvitedByUserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? UsedByUserId { get; set; }
    public string RequestedCorrelationId { get; set; } = string.Empty;

    public bool IsAvailable(DateTime utcNow) => UsedAt is null && !IsDeleted && ExpiresAt > utcNow;

    public void Use(DateTime utcNow, Guid usedByUserId)
    {
        if (!IsAvailable(utcNow)) throw new InvalidOperationException("Convite indisponível ou expirado.");
        UsedAt = utcNow;
        UsedByUserId = usedByUserId;
        Touch();
    }

    /// <summary>Revogação por reemissão/ação do admin: expira no ato, sem apagar histórico.</summary>
    public void Revoke(DateTime utcNow)
    {
        if (UsedAt.HasValue) return; // convite já usado é histórico intacto
        ExpiresAt = utcNow;
        Touch();
    }
}
