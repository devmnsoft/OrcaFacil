using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public static class BrazilianDocument
{
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(char.IsDigit).ToArray());

    public static bool HasBasicValidLength(BrazilianDocumentType? type, string? numbers)
    {
        if (type is null || string.IsNullOrWhiteSpace(numbers)) return false;
        return type == BrazilianDocumentType.CNPJ ? numbers.Length == 14 : numbers.Length == 11;
    }

    public static bool HasValidCheckDigits(BrazilianDocumentType? type, string? value)
    {
        if (!HasOnlySupportedCharacters(value)) return false;
        var numbers = Normalize(value);
        if (type is null || string.IsNullOrWhiteSpace(numbers)) return false;
        return type == BrazilianDocumentType.CNPJ ? IsValidCnpj(numbers) : IsValidCpf(numbers);
    }

    private static bool HasOnlySupportedCharacters(string? value) =>
        value is null || value.All(character => char.IsDigit(character) ||
            char.IsWhiteSpace(character) || character is '.' or '-' or '/');

    public static string Mask(BrazilianDocumentType? type, string? numbers)
    {
        numbers = Normalize(numbers);
        if (string.IsNullOrWhiteSpace(numbers)) return "Não informado";
        if (type == BrazilianDocumentType.CNPJ && numbers.Length == 14) return $"{numbers[..2]}.***.***/****-{numbers[12..]}";
        if (numbers.Length == 11) return $"***.***.***-{numbers[9..]}";
        return "Documento mascarado";
    }

    private static bool IsValidCpf(string numbers)
    {
        if (numbers.Length != 11 || numbers.Distinct().Count() == 1) return false;
        var first = CalculateCpfDigit(numbers, 9);
        var second = CalculateCpfDigit(numbers, 10);
        return numbers[9] == (char)('0' + first) && numbers[10] == (char)('0' + second);
    }

    private static int CalculateCpfDigit(string numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++) sum += (numbers[i] - '0') * (length + 1 - i);
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool IsValidCnpj(string numbers)
    {
        if (numbers.Length != 14 || numbers.Distinct().Count() == 1) return false;
        int[] firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var first = CalculateCnpjDigit(numbers, firstWeights);
        var second = CalculateCnpjDigit(numbers, secondWeights);
        return numbers[12] == (char)('0' + first) && numbers[13] == (char)('0' + second);
    }

    private static int CalculateCnpjDigit(string numbers, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++) sum += (numbers[i] - '0') * weights[i];
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
