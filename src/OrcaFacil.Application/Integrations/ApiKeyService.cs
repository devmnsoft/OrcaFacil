using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Integrations;

public sealed record CreatedApiKey(ApiKey Entity, string PlaintextKey);

public interface IApiKeyService
{
    CreatedApiKey Create(Guid accountId, Guid userId, string name, IEnumerable<string> scopes, DateTime? expiresAt);
    bool Verify(string plaintextKey, ApiKey stored);
}

public sealed class ApiKeyService : IApiKeyService
{
    public static readonly IReadOnlySet<string> AllowedScopes = new HashSet<string>(StringComparer.Ordinal)
    { "clients.read", "clients.write", "quotes.read", "quotes.write", "payments.read", "receipts.read", "webhooks.write" };

    public CreatedApiKey Create(Guid accountId, Guid userId, string name, IEnumerable<string> scopes, DateTime? expiresAt)
    {
        var selected = scopes.Distinct(StringComparer.Ordinal).ToArray();
        if (selected.Length == 0 || selected.Any(x => !AllowedScopes.Contains(x))) throw new ArgumentException("Selecione somente escopos permitidos.", nameof(scopes));
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var plaintext = $"of_live_{raw}";
        return new CreatedApiKey(new ApiKey { AccountId = accountId, CreatedByUserId = userId, Name = name.Trim(), Prefix = plaintext[..16], KeyHash = Hash(plaintext), Scopes = string.Join(',', selected), ExpiresAt = expiresAt }, plaintext);
    }

    public bool Verify(string plaintextKey, ApiKey stored)
    {
        if (stored.RevokedAt.HasValue || stored.ExpiresAt <= DateTime.UtcNow) return false;
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Hash(plaintextKey)), Convert.FromHexString(stored.KeyHash));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
