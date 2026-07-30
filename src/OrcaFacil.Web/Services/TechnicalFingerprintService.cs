using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Web.Services;

public sealed class TechnicalFingerprintService(IConfiguration configuration) : ITechnicalFingerprintService
{
    public string Create(string value)
    {
        var pepper = configuration["Security:TechnicalFingerprintPepper"];
        if (string.IsNullOrWhiteSpace(pepper))
            throw new InvalidOperationException("Security:TechnicalFingerprintPepper must be configured.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
    }
}
