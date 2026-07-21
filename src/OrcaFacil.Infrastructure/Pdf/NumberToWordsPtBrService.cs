using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Infrastructure.Pdf;

public class NumberToWordsPtBrService : INumberToWordsService
{
    public string ToCurrencyWords(decimal value)
    {
        var reais = (long)Math.Floor(value);
        var centavos = (int)Math.Round((value - reais) * 100, 0);
        var text = reais == 1 ? "um real" : $"{Number(reais)} reais";
        if (centavos > 0)
        {
            text += centavos == 1 ? " e um centavo" : $" e {Number(centavos)} centavos";
        }
        return text;
    }

    private static string Number(long value)
    {
        if (value == 0) return "zero";
        if (value == 1) return "um";
        if (value == 2) return "dois";
        return value.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
    }
}
