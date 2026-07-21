using System.Security.Cryptography;

namespace OrcaFacil.Domain.ValueObjects;

public sealed record PublicToken
{
    public string Value { get; }

    public PublicToken(string? value = null)
    {
        Value = string.IsNullOrWhiteSpace(value)
            ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=')
            : value.Trim();

        if (Value.Length < 32)
        {
            throw new ArgumentException("Token público inseguro.", nameof(value));
        }
    }
}
