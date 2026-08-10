using System.Text.RegularExpressions;

namespace OrcaFacil.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("E-mail inválido.", nameof(value));
        }

        var normalized = value.Trim();
        if (!Regex.IsMatch(normalized, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("E-mail inválido.", nameof(value));
        }

        Value = normalized.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
