using System.Globalization;
using System.Text;

namespace OrcaFacil.Domain.Enums;

public enum PaymentMethodCode
{
    Pix,
    Cash,
    Transfer,
    Card,
    Boleto,
    Other
}

public static class PaymentMethodCodes
{
    public static bool TryParse(string? value, out PaymentMethodCode code)
    {
        var normalized = RemoveDiacritics(value).Trim().ToLowerInvariant();
        code = normalized switch
        {
            "pix" => PaymentMethodCode.Pix,
            "cash" or "dinheiro" => PaymentMethodCode.Cash,
            "transfer" or "transferencia" or "transferencia bancaria" => PaymentMethodCode.Transfer,
            "card" or "cartao" or "cartao de credito" or "cartao de debito" => PaymentMethodCode.Card,
            "boleto" or "bank slip" => PaymentMethodCode.Boleto,
            "other" or "outro" => PaymentMethodCode.Other,
            _ => default
        };
        return normalized is "pix" or "cash" or "dinheiro" or "transfer" or "transferencia" or
            "transferencia bancaria" or "card" or "cartao" or "cartao de credito" or
            "cartao de debito" or "boleto" or "bank slip" or "other" or "outro";
    }

    public static string ToCode(this PaymentMethodCode code) => code switch
    {
        PaymentMethodCode.Pix => "pix",
        PaymentMethodCode.Cash => "cash",
        PaymentMethodCode.Transfer => "transfer",
        PaymentMethodCode.Card => "card",
        PaymentMethodCode.Boleto => "boleto",
        PaymentMethodCode.Other => "other",
        _ => throw new ArgumentOutOfRangeException(nameof(code))
    };

    public static string ToLabel(this PaymentMethodCode code) => code switch
    {
        PaymentMethodCode.Pix => "Pix",
        PaymentMethodCode.Cash => "Dinheiro",
        PaymentMethodCode.Transfer => "Transferência",
        PaymentMethodCode.Card => "Cartão",
        PaymentMethodCode.Boleto => "Boleto",
        PaymentMethodCode.Other => "Outro",
        _ => throw new ArgumentOutOfRangeException(nameof(code))
    };

    public static string ToIconName(this PaymentMethodCode code) => code == PaymentMethodCode.Other
        ? "payment"
        : code.ToCode();

    public static string ToDisplayLabel(string? storedValue) =>
        TryParse(storedValue, out var code) ? code.ToLabel() : storedValue?.Trim() ?? string.Empty;

    private static string RemoveDiacritics(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Normalize(NormalizationForm.FormD);
        return string.Concat(decomposed.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
    }
}
