using System.ComponentModel.DataAnnotations;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.ValueObjects;

namespace OrcaFacil.Application.Auth;

public enum LoginIdentifierKind { Cpf, Cnpj, Email }
public sealed record NormalizedLoginIdentifier(LoginIdentifierKind Kind, string Value);

public sealed class BrazilianDocumentNormalizer
{
    public string Normalize(string value, BrazilianDocumentType type)
    {
        var normalized = BrazilianDocument.Normalize(value);
        if (normalized is null || !BrazilianDocument.HasBasicValidLength(type, normalized) || !BrazilianDocument.HasValidCheckDigits(type, normalized))
            throw new ArgumentException(type == BrazilianDocumentType.CPF ? "Informe um CPF válido." : "Informe um CNPJ válido.");
        return normalized;
    }
}

public sealed class InstitutionalEmailValidator
{
    private static readonly HashSet<string> PublicDomains = new(StringComparer.OrdinalIgnoreCase)
        { "gmail.com", "hotmail.com", "outlook.com", "yahoo.com", "icloud.com", "uol.com.br", "bol.com.br" };
    public bool IsValid(string email, bool commonEmailAllowed = true)
    {
        if (!new EmailAddressAttribute().IsValid(email)) return false;
        var domain = email[(email.LastIndexOf('@') + 1)..];
        return commonEmailAllowed || !PublicDomains.Contains(domain);
    }
}

public sealed class LoginIdentifierService(BrazilianDocumentNormalizer documents, InstitutionalEmailValidator emails)
{
    public NormalizedLoginIdentifier Normalize(string value, bool commonEmailAllowed = true)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Informe seu CPF, CNPJ ou e-mail.");
        var trimmed = value.Trim();
        if (trimmed.Contains('@'))
        {
            var email = new Email(trimmed).Value.ToLowerInvariant();
            if (!emails.IsValid(email, commonEmailAllowed)) throw new ArgumentException("Use um e-mail institucional válido.");
            return new(LoginIdentifierKind.Email, email);
        }
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length switch
        {
            11 => new(LoginIdentifierKind.Cpf, documents.Normalize(digits, BrazilianDocumentType.CPF)),
            14 => new(LoginIdentifierKind.Cnpj, documents.Normalize(digits, BrazilianDocumentType.CNPJ)),
            _ => throw new ArgumentException("Informe um CPF, CNPJ ou e-mail válido.")
        };
    }
}
