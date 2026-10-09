using System.Text;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Localization;

namespace OrcaFacil.Infrastructure.Pdf;

public class NumberToWordsPtBrService : INumberToWordsService
{
    // --- Português ---
    private static readonly string[] PtUnidades =
    [
        "zero", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove",
        "dez", "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove"
    ];

    private static readonly string[] PtDezenas =
    [
        "", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"
    ];

    private static readonly string[] PtCentenas =
    [
        "", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"
    ];

    // --- Inglês ---
    private static readonly string[] EnUnits =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
    ];

    private static readonly string[] EnTens =
    [
        "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
    ];

    // --- Espanhol ---
    private static readonly string[] EsUnidades =
    [
        "cero", "un", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
        "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve",
        "veinte", "veintiuno", "veintidós", "veintitrés", "veinticuatro", "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve"
    ];

    private static readonly string[] EsDezenas =
    [
        "", "", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"
    ];

    private static readonly string[] EsCentenas =
    [
        "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos"
    ];

    public string ToCurrencyWords(decimal value) => ToCurrencyWords(value, SupportedLocales.Default, "BRL");

    public string ToCurrencyWords(decimal value, string languageCode, string currencyCode = "BRL")
    {
        var normLang = SupportedLocales.Normalize(languageCode);
        var normCurr = string.IsNullOrWhiteSpace(currencyCode) ? "BRL" : currencyCode.Trim().ToUpperInvariant();

        return normLang switch
        {
            "en-US" => ConvertEnglish(value, normCurr),
            "es-ES" or "es-419" => ConvertSpanish(value, normCurr),
            _ => ConvertPortuguese(value, normCurr)
        };
    }

    #region Português

    private static string ConvertPortuguese(decimal value, string currency)
    {
        var (singularUnit, pluralUnit, singularSub, pluralSub) = currency switch
        {
            "USD" => ("dólar americano", "dólares americanos", "centavo", "centavos"),
            "EUR" => ("euro", "euros", "centavo", "centavos"),
            _ => ("real", "reais", "centavo", "centavos")
        };

        if (value == 0) return $"zero {singularUnit}";

        var positive = Math.Abs(value);
        var intPart = (long)Math.Floor(positive);
        var centsPart = (int)Math.Round((positive - intPart) * 100, 0);

        var sb = new StringBuilder();
        if (intPart > 0)
        {
            sb.Append(ConverterPt(intPart));
            sb.Append(intPart == 1 ? $" {singularUnit}" : $" {pluralUnit}");
        }

        if (centsPart > 0)
        {
            if (intPart > 0) sb.Append(" e ");
            sb.Append(ConverterPt(centsPart));
            sb.Append(centsPart == 1 ? $" {singularSub}" : $" {pluralSub}");
        }

        return sb.ToString();
    }

    private static string ConverterPt(long n)
    {
        if (n == 0) return "zero";
        if (n < 20) return PtUnidades[n];
        if (n < 100)
        {
            var dez = PtDezenas[n / 10];
            var resto = n % 10;
            return resto == 0 ? dez : $"{dez} e {PtUnidades[resto]}";
        }
        if (n == 100) return "cem";
        if (n < 1000)
        {
            var cen = PtCentenas[n / 100];
            var resto = n % 100;
            return resto == 0 ? cen : $"{cen} e {ConverterPt(resto)}";
        }
        if (n < 1000000)
        {
            var mil = n / 1000;
            var resto = n % 1000;
            var milText = mil == 1 ? "um mil" : $"{ConverterPt(mil)} mil";
            if (resto == 0) return milText;
            var sep = (resto < 100 || resto % 100 == 0) ? " e " : " e ";
            return $"{milText}{sep}{ConverterPt(resto)}";
        }
        if (n < 1000000000)
        {
            var milhao = n / 1000000;
            var resto = n % 1000000;
            var milhaoText = milhao == 1 ? "um milhão" : $"{ConverterPt(milhao)} milhões";
            if (resto == 0) return milhaoText;
            var sep = (resto < 100 || resto % 100 == 0) ? " e " : ", ";
            return $"{milhaoText}{sep}{ConverterPt(resto)}";
        }
        return n.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
    }

    #endregion

    #region Inglês

    private static string ConvertEnglish(decimal value, string currency)
    {
        // Nunca apresentar dólares para BRL
        var (singularUnit, pluralUnit, singularSub, pluralSub) = currency switch
        {
            "USD" => ("US dollar", "US dollars", "cent", "cents"),
            "EUR" => ("euro", "euros", "cent", "cents"),
            _ => ("Brazilian real", "Brazilian reals", "cent", "cents")
        };

        if (value == 0) return $"zero {pluralUnit}";

        var positive = Math.Abs(value);
        var intPart = (long)Math.Floor(positive);
        var centsPart = (int)Math.Round((positive - intPart) * 100, 0);

        var sb = new StringBuilder();
        if (intPart > 0)
        {
            sb.Append(ConverterEn(intPart));
            sb.Append(intPart == 1 ? $" {singularUnit}" : $" {pluralUnit}");
        }

        if (centsPart > 0)
        {
            if (intPart > 0) sb.Append(" and ");
            sb.Append(ConverterEn(centsPart));
            sb.Append(centsPart == 1 ? $" {singularSub}" : $" {pluralSub}");
        }

        return sb.ToString();
    }

    private static string ConverterEn(long n)
    {
        if (n == 0) return "zero";
        if (n < 20) return EnUnits[n];
        if (n < 100)
        {
            var tens = EnTens[n / 10];
            var resto = n % 10;
            return resto == 0 ? tens : $"{tens}-{EnUnits[resto]}";
        }
        if (n < 1000)
        {
            var hundred = $"{EnUnits[n / 100]} hundred";
            var resto = n % 100;
            return resto == 0 ? hundred : $"{hundred} and {ConverterEn(resto)}";
        }
        if (n < 1000000)
        {
            var mil = n / 1000;
            var resto = n % 1000;
            var milText = $"{ConverterEn(mil)} thousand";
            if (resto == 0) return milText;
            var sep = resto < 100 ? " and " : ", ";
            return $"{milText}{sep}{ConverterEn(resto)}";
        }
        if (n < 1000000000)
        {
            var milhao = n / 1000000;
            var resto = n % 1000000;
            var milhaoText = $"{ConverterEn(milhao)} million";
            if (resto == 0) return milhaoText;
            var sep = resto < 100 ? " and " : ", ";
            return $"{milhaoText}{sep}{ConverterEn(resto)}";
        }
        return n.ToString("N0", new System.Globalization.CultureInfo("en-US"));
    }

    #endregion

    #region Espanhol

    private static string ConvertSpanish(decimal value, string currency)
    {
        var (singularUnit, pluralUnit, singularSub, pluralSub) = currency switch
        {
            "USD" => ("dólar estadounidense", "dólares estadounidenses", "centavo", "centavos"),
            "EUR" => ("euro", "euros", "céntimo", "céntimos"),
            _ => ("real brasileño", "reales brasileños", "centavo", "centavos")
        };

        if (value == 0) return $"cero {pluralUnit}";

        var positive = Math.Abs(value);
        var intPart = (long)Math.Floor(positive);
        var centsPart = (int)Math.Round((positive - intPart) * 100, 0);

        var sb = new StringBuilder();
        if (intPart > 0)
        {
            sb.Append(ConverterEs(intPart));
            sb.Append(intPart == 1 ? $" {singularUnit}" : $" {pluralUnit}");
        }

        if (centsPart > 0)
        {
            if (intPart > 0) sb.Append(" con ");
            sb.Append(ConverterEs(centsPart));
            sb.Append(centsPart == 1 ? $" {singularSub}" : $" {pluralSub}");
        }

        return sb.ToString();
    }

    private static string ConverterEs(long n)
    {
        if (n == 0) return "cero";
        if (n <= 29) return EsUnidades[n];
        if (n < 100)
        {
            var dez = EsDezenas[n / 10];
            var resto = n % 10;
            return resto == 0 ? dez : $"{dez} y {EsUnidades[resto]}";
        }
        if (n == 100) return "cien";
        if (n < 1000)
        {
            var cen = EsCentenas[n / 100];
            var resto = n % 100;
            return resto == 0 ? cen : $"{cen} {ConverterEs(resto)}";
        }
        if (n < 1000000)
        {
            var mil = n / 1000;
            var resto = n % 1000;
            var milText = mil == 1 ? "mil" : $"{ConverterEs(mil)} mil";
            if (resto == 0) return milText;
            return $"{milText} {ConverterEs(resto)}";
        }
        if (n < 1000000000)
        {
            var milhao = n / 1000000;
            var resto = n % 1000000;
            var milhaoText = milhao == 1 ? "un millón" : $"{ConverterEs(milhao)} millones";
            if (resto == 0) return milhaoText;
            return $"{milhaoText} {ConverterEs(resto)}";
        }
        return n.ToString("N0", new System.Globalization.CultureInfo("es-ES"));
    }

    #endregion
}
