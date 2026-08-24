using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Partners;

public sealed record IssuedPartnerToken(string RawToken, string TokenHash, DateTime ExpiresAt);

/// <summary>Issues opaque, single-use portal credentials. Only the SHA-256 digest is persisted.</summary>
public sealed class PartnerTokenService
{
    public IssuedPartnerToken Issue(TimeSpan lifetime, DateTime? now = null)
    {
        if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new(raw, Hash(raw), (now ?? DateTime.UtcNow).Add(lifetime));
    }

    public bool Matches(string rawToken, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(expectedHash)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(Hash(rawToken)), Convert.FromHexString(expectedHash));
    }

    public static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

public static class PartnerAccessService
{
    public static void Demand(Guid accountId, Guid partnerId, Guid resourceAccountId, Guid resourcePartnerId)
    {
        if (accountId == Guid.Empty || partnerId == Guid.Empty || accountId != resourceAccountId || partnerId != resourcePartnerId)
            throw new UnauthorizedAccessException("O recurso não pertence ao parceiro autenticado.");
    }

    public static void DemandActive(PartnerPortalUser user, DateTime now)
    {
        if (!user.IsActive || user.AccessRevokedAt is not null && user.AccessRevokedAt <= now)
            throw new UnauthorizedAccessException("O acesso ao portal foi revogado.");
    }
}

public static class PartnerPortalInvitationService
{
    public static void ValidateForAcceptance(PartnerPortalInvitation invitation, string rawToken, PartnerTokenService tokens, DateTime now)
    {
        if (invitation.RevokedAt is not null) throw new InvalidOperationException("O convite foi revogado.");
        if (invitation.AcceptedAt is not null) throw new InvalidOperationException("O convite já foi utilizado.");
        if (invitation.ExpiresAt <= now) throw new InvalidOperationException("O convite expirou.");
        if (!tokens.Matches(rawToken, invitation.TokenHash)) throw new UnauthorizedAccessException("Convite inválido.");
    }
}
