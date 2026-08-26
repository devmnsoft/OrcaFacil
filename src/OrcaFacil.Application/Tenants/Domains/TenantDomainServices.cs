using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Tenants.Domains;

public interface ITenantDomainStore
{
    Task<bool> HostExistsAsync(string normalizedHost, Guid? excludingId, CancellationToken cancellationToken);
    Task<TenantDomain?> FindActiveByHostAsync(string normalizedHost, CancellationToken cancellationToken);
    Task SaveAsync(TenantDomain domain, CancellationToken cancellationToken);
    Task AddVerificationAsync(TenantDomainVerification verification, CancellationToken cancellationToken);
}

public sealed record DomainVerificationChallenge(string RecordType, string RecordName, string RecordValue, int RecommendedTtl);
public sealed record TenantHostResolution(bool Resolved, Guid? AccountId, Guid? DomainId, string NormalizedHost, string Reason);

public sealed class TenantDomainService(ITenantDomainStore store)
{
    private static readonly string[] ReservedSuffixes = ["orcafacil.com.br", "orcafacil.app"];

    public async Task<TenantDomain> CreateAsync(Guid accountId, string host, TenantDomainType type, Guid userId, CancellationToken cancellationToken = default)
    {
        var normalized = TenantDomain.NormalizeHost(host);
        if (ReservedSuffixes.Any(x => normalized == x || normalized.EndsWith('.' + x, StringComparison.Ordinal)))
            throw new InvalidOperationException("Este domínio é reservado pelo sistema.");
        if (await store.HostExistsAsync(normalized, null, cancellationToken))
            throw new InvalidOperationException("Este domínio já está vinculado a uma conta.");
        var domain = new TenantDomain(accountId, host, type, userId);
        await store.SaveAsync(domain, cancellationToken);
        return domain;
    }
}

public sealed class TenantDomainVerificationService(ITenantDomainStore store)
{
    public (string RawToken, DomainVerificationChallenge Challenge) IssueChallenge(TenantDomain domain)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        domain.SetVerification(Hash(token), TenantDomainVerificationMethod.Txt);
        return (token, new DomainVerificationChallenge("TXT", $"_orcafacil-verification.{domain.NormalizedHostName}", token, 300));
    }

    public async Task RecordDnsResultAsync(TenantDomain domain, string presentedToken, Guid? actorId, CancellationToken cancellationToken = default)
    {
        var storedHash = domain.VerificationTokenHash;
        var success = !string.IsNullOrWhiteSpace(presentedToken) && storedHash is { Length: 64 } &&
                      CryptographicOperations.FixedTimeEquals(Convert.FromHexString(storedHash), Convert.FromHexString(Hash(presentedToken)));
        domain.RecordCheck(success, success ? "DnsTxtMatched" : "DnsTxtNotFound");
        await store.AddVerificationAsync(new TenantDomainVerification
        {
            AccountId = domain.AccountId, TenantDomainId = domain.Id, Method = TenantDomainVerificationMethod.Txt,
            Succeeded = success, ResultCode = success ? "DnsTxtMatched" : "DnsTxtNotFound",
            FailureReason = success ? null : "O registro TXT esperado não foi encontrado.", ApprovedByUserId = actorId
        }, cancellationToken);
        await store.SaveAsync(domain, cancellationToken);
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class TenantHostResolver(ITenantDomainStore store)
{
    public async Task<TenantHostResolution> ResolveAsync(string host, CancellationToken cancellationToken = default)
    {
        string normalized;
        try { normalized = TenantDomain.NormalizeHost(host); }
        catch (ArgumentException) { return new(false, null, null, string.Empty, "MalformedHost"); }
        var domain = await store.FindActiveByHostAsync(normalized, cancellationToken);
        return domain is null
            ? new(false, null, null, normalized, "UnknownOrInactiveHost")
            : new(true, domain.AccountId, domain.Id, normalized, "ActiveVerifiedDomain");
    }
}

public sealed class TenantPublicUrlService
{
    public Uri Build(string path, TenantDomain? domain, string publicBaseUrl)
    {
        var baseUri = domain?.Status == TenantDomainStatus.Active
            ? new Uri($"https://{domain.NormalizedHostName}", UriKind.Absolute)
            : RequireSafeBaseUrl(publicBaseUrl);
        return new Uri(baseUri, '/' + (path ?? string.Empty).TrimStart('/'));
    }

    private static Uri RequireSafeBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.IsLoopback)
            throw new InvalidOperationException("PublicBaseUrl deve ser uma URL HTTPS pública válida.");
        return uri;
    }
}

public static class WhiteLabelEntitlements
{
    public const string CustomDomain = "WhiteLabel.CustomDomain";
    public const string CustomBranding = "WhiteLabel.CustomBranding";
    public const string PortalDomain = "WhiteLabel.PortalDomain";
    public const string ApiDomain = "WhiteLabel.ApiDomain";
    public const string EmailDomain = "WhiteLabel.EmailDomain";
    public const string RemoveOrcaFacilBrand = "WhiteLabel.RemoveOrcaFacilBrand";
}
