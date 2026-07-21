using System.Text.RegularExpressions;

namespace OrcaFacil.Domain.ValueObjects;

public sealed record CpfCnpj
{
    public string Value { get; }

    public CpfCnpj(string value)
    {
        var digits = Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
        if (digits.Length is not (11 or 14))
        {
            throw new ArgumentException("CPF/CNPJ deve ter 11 ou 14 dígitos.", nameof(value));
        }

        Value = digits;
    }
}
