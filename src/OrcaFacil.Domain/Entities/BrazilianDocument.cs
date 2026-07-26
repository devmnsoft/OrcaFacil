using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public static class BrazilianDocument
{
    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(char.IsDigit).ToArray());

    public static bool HasBasicValidLength(BrazilianDocumentType? type, string? numbers)
    {
        if (string.IsNullOrWhiteSpace(numbers)) return true;
        return type == BrazilianDocumentType.CNPJ ? numbers.Length == 14 : numbers.Length == 11;
    }

    public static bool HasValidCheckDigits(BrazilianDocumentType? type, string? numbers)
    {
        numbers = Normalize(numbers);
        if (string.IsNullOrWhiteSpace(numbers)) return true;
        return type == BrazilianDocumentType.CNPJ ? IsValidCnpj(numbers) : IsValidCpf(numbers);
    }

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
        var sum = 0;
        for (var i = 0; i < 9; i++) sum += (numbers[i] - '0') * (10 - i);
        var digit = sum % 11 < 2 ? 0 : 11 - sum % 11;
        if (digit != numbers[9] - '0') return false;
        sum = 0;
        for (var i = 0; i < 10; i++) sum += (numbers[i] - '0') * (11 - i);
        digit = sum % 11 < 2 ? 0 : 11 - sum % 11;
        return digit == numbers[10] - '0';
    }

    private static bool IsValidCnpj(string numbers)
    {
        if (numbers.Length != 14 || numbers.Distinct().Count() == 1) return false;
        int[] firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        return CalculateCnpjDigit(numbers, firstWeights, 12) == numbers[12] - '0'
            && CalculateCnpjDigit(numbers, secondWeights, 13) == numbers[13] - '0';
    }

    private static int CalculateCnpjDigit(string numbers, int[] weights, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++) sum += (numbers[i] - '0') * weights[i];
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
