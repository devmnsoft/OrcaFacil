using System.Text.RegularExpressions;

namespace OrcaFacil.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("E-mail inválido.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    public override string ToString() => Value;
}
