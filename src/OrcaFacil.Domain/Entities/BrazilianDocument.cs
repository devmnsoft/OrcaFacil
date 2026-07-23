using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public static class BrazilianDocument
{
    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(char.IsDigit).ToArray());
    public static bool HasBasicValidLength(DocumentType? type, string? numbers)
    {
        if (string.IsNullOrWhiteSpace(numbers)) return true;
        return type == DocumentType.CNPJ ? numbers.Length == 14 : numbers.Length == 11;
    }
    public static bool HasValidCheckDigits(DocumentType type, string numbers) => HasBasicValidLength(type, numbers); // preparado para algoritmo completo.
    public static string Mask(DocumentType? type, string? numbers)
    {
        numbers = Normalize(numbers);
        if (string.IsNullOrWhiteSpace(numbers)) return "Não informado";
        if (type == DocumentType.CNPJ && numbers.Length == 14) return $"{numbers[..2]}.***.***/****-{numbers[12..]}";
        if (numbers.Length == 11) return $"***.***.***-{numbers[9..]}";
        return "Documento mascarado";
    }
}
