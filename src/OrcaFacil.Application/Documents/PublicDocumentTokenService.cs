using System.Security.Cryptography;
using System.Text;

namespace OrcaFacil.Application.Documents;

public interface IPublicDocumentTokenService
{
    (string Token, string Hash) Create();
    string Hash(string token);
    bool Matches(string token, string expectedHash);
}

public sealed class PublicDocumentTokenService : IPublicDocumentTokenService
{
    public (string Token, string Hash) Create()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, Hash(token));
    }

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public bool Matches(string token, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expectedHash)) return false;
        var actual = Encoding.ASCII.GetBytes(Hash(token));
        var expected = Encoding.ASCII.GetBytes(expectedHash.ToUpperInvariant());
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
