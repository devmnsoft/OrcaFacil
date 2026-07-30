using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Persistence.Services;

public sealed class TechnicalFingerprintService : ITechnicalFingerprintService
{
    private readonly byte[] pepper;

    public TechnicalFingerprintService(string pepper)
    {
        if (string.IsNullOrWhiteSpace(pepper) || pepper.Length < 32)
            throw new ArgumentException("O segredo de fingerprint técnico deve possuir ao menos 32 caracteres.", nameof(pepper));
        this.pepper = Encoding.UTF8.GetBytes(pepper);
    }

    public string Create(string value)
    {
        using var hmac = new HMACSHA256(pepper);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
    }
}
