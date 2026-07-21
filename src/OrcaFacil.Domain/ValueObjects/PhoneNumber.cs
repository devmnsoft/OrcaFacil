using System.Text.RegularExpressions;

namespace OrcaFacil.Domain.ValueObjects;

public sealed record PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        var digits = Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
        if (digits.Length < 10 || digits.Length > 13)
        {
            throw new ArgumentException("Telefone inválido.", nameof(value));
        }

        Value = digits;
    }
}
