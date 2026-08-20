using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Privacy;

public sealed class ConsentService(IRepository<PrivacyConsent> consents, IUnitOfWork unitOfWork, IAuditService audit)
{
    public async Task<PrivacyConsent> AcceptAsync(Guid accountId, Guid userId, PrivacyConsentType type, string version,
        string ipAddress, string userAgent, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Conta, usuário e versão são obrigatórios.");
        var consent = new PrivacyConsent { AccountId = accountId, UserId = userId, ConsentType = type,
            Version = version.Trim(), AcceptedAt = DateTime.UtcNow, IpAddress = ipAddress, UserAgent = userAgent };
        await consents.AddAsync(consent, ct);
        await audit.RegisterAsync(userId, "Privacy.ConsentAccepted", nameof(PrivacyConsent), consent.Id.ToString(), null,
            new { consent.ConsentType, consent.Version, consent.AcceptedAt }, null, ct, accountId);
        await unitOfWork.SaveChangesAsync(ct);
        return consent;
    }

    public async Task RevokeAsync(Guid accountId, Guid userId, Guid consentId, CancellationToken ct = default)
    {
        var consent = await consents.GetAsync(consentId, ct);
        if (consent is null || consent.AccountId != accountId || consent.UserId != userId || consent.IsDeleted)
            throw new UnauthorizedAccessException("Consentimento não pertence ao usuário e à conta informados.");
        if (consent.RevokedAt is null) { consent.RevokedAt = DateTime.UtcNow; consent.Touch();
            await audit.RegisterAsync(userId, "Privacy.ConsentRevoked", nameof(PrivacyConsent), consent.Id.ToString(), null,
                new { consent.ConsentType, consent.Version, consent.RevokedAt }, null, ct, accountId);
            await unitOfWork.SaveChangesAsync(ct); }
    }

    public bool HasCurrentMandatoryConsent(Guid accountId, Guid userId, PrivacyConsentType type, string currentVersion) =>
        consents.Query().Any(x => x.AccountId == accountId && x.UserId == userId && x.ConsentType == type &&
            x.Version == currentVersion && x.RevokedAt == null && !x.IsDeleted);
}
