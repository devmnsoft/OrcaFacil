using System.Security.Cryptography;
using System.Text;

namespace OrcaFacil.Application.Omnichannel;

/// <summary>
/// Verificação da assinatura HMAC-SHA256 do Meta (`X-Hub-Signature-256`, formato
/// `sha256=&lt;hex&gt;`). Pura (sem IO) para ser coberta por teste: rejeita header
/// ausente, prefixo errado, segredo vazio e compara em tempo constante.
/// </summary>
public static class WhatsAppSignature
{
    public const string HeaderName = "X-Hub-Signature-256";
    private const string Prefix = "sha256=";

    public static bool Verify(string appSecret, string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(appSecret) || string.IsNullOrWhiteSpace(rawBody)) return false;
        if (string.IsNullOrWhiteSpace(signatureHeader)) return false;
        if (!signatureHeader.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var expectedHex = signatureHeader[Prefix.Length..].Trim();
        if (expectedHex.Length == 0) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var actualHex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHex),
            Encoding.UTF8.GetBytes(expectedHex.ToLowerInvariant()));
    }

    /// <summary>
    /// Normaliza o telefone para o formato E.164 numérico (sem "+") que a Cloud API
    /// exige no campo <c>to</c>. Número nacional com DDD (10 ou 11 dígitos) recebe o
    /// DDI 55; número já internacional passa adiante sem adivinhar o país.
    /// O comprimento >= 12 é o desempate do "55": "55 99999-8888" é DDD 55 local
    /// (11 dígitos) e vira 5555..., enquanto "551133334444" (12 dígitos) já é internacional.
    /// </summary>
    public static string NormalizePhone(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray()).TrimStart('0');
        if (digits.Length == 0) return string.Empty;
        var alreadyInternational = digits.StartsWith("55", StringComparison.Ordinal) && digits.Length >= 12;
        var brazilianLocal = digits.Length is 10 or 11;
        return alreadyInternational || !brazilianLocal ? digits : $"55{digits}";
    }
}
