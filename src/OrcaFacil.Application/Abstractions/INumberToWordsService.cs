namespace OrcaFacil.Application.Abstractions;

public interface INumberToWordsService
{
    string ToCurrencyWords(decimal value);
    string ToCurrencyWords(decimal value, string languageCode, string currencyCode = "BRL");
}
