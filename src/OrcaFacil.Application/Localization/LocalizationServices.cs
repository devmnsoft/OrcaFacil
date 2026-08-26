using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OrcaFacil.Application.Localization;

public static class SupportedLocales
{
    public const string Default = "pt-BR";
    public static readonly IReadOnlyDictionary<string, LocalizationLanguage> All =
        new[]
        {
            new LocalizationLanguage("pt-BR", "Portuguese (Brazil)", "Português (Brasil)", true, true, true, true, true),
            new LocalizationLanguage("en-US", "English (United States)", "English (United States)", false, true, true, true, true),
            new LocalizationLanguage("es-ES", "Spanish (Spain)", "Español (España)", false, true, true, true, true),
            new LocalizationLanguage("es-419", "Spanish (Latin America)", "Español (Latinoamérica)", false, true, true, true, true)
        }.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? code) =>
        code is not null && All.TryGetValue(code.Trim(), out var language) ? language.Code : Default;
}

public sealed record LocalizationLanguage(string Code, string Name, string NativeName, bool IsDefault,
    bool IsActive, bool IsPublicEnabled, bool IsPortalEnabled, bool IsAdminEnabled);

public sealed record LocaleSettings(string LanguageCode, string CultureCode, string CurrencyCode,
    string TimeZoneId, string DateFormat = "d", string TimeFormat = "t");

public sealed class LocalePreferenceService
{
    public LocaleSettings Resolve(LocaleSettings? user, LocaleSettings? portal, LocaleSettings? account)
    {
        var selected = user ?? portal ?? account ?? Defaults();
        var language = SupportedLocales.Normalize(selected.LanguageCode);
        var culture = TryCulture(selected.CultureCode, language);
        var currency = selected.CurrencyCode is "BRL" or "USD" or "EUR" ? selected.CurrencyCode : "BRL";
        var timeZone = TryTimeZone(selected.TimeZoneId);
        return selected with { LanguageCode = language, CultureCode = culture, CurrencyCode = currency, TimeZoneId = timeZone };
    }

    public static LocaleSettings Defaults() => new(SupportedLocales.Default, "pt-BR", "BRL", "America/Sao_Paulo");

    private static string TryCulture(string? value, string fallback)
    {
        try { return CultureInfo.GetCultureInfo(value ?? fallback).Name; }
        catch (CultureNotFoundException) { return fallback; }
    }

    private static string TryTimeZone(string? value)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(value ?? "UTC").Id; }
        catch (TimeZoneNotFoundException) { return "UTC"; }
        catch (InvalidTimeZoneException) { return "UTC"; }
    }
}

public sealed class RegionalFormatService
{
    public string FormatCurrency(decimal value, string cultureCode, string currencyCode)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(cultureCode).Clone();
        culture.NumberFormat.CurrencySymbol = currencyCode switch { "BRL" => "R$", "USD" => "US$", "EUR" => "€", _ => currencyCode };
        return value.ToString("C", culture); // Formatting never changes the stored amount.
    }

    public string FormatDateTime(DateTime utcValue, string cultureCode, string timeZoneId, string format = "g")
    {
        var utc = utcValue.Kind == DateTimeKind.Utc ? utcValue : DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        return local.ToString(format, CultureInfo.GetCultureInfo(cultureCode));
    }
}

public sealed record TranslationEntry(string Key, string Value, string LanguageCode, string Module, bool IsPublished);
public sealed record TranslationImportPreview(IReadOnlyList<TranslationEntry> Valid, IReadOnlyList<string> Errors, int Conflicts);

public sealed partial class TranslationImportService
{
    [GeneratedRegex("<\\s*(script|iframe)|javascript:", RegexOptions.IgnoreCase)]
    private static partial Regex UnsafeMarkup();

    public TranslationImportPreview PreviewJson(string json, IReadOnlySet<string> knownKeys, IReadOnlySet<string> existingKeys)
    {
        var valid = new List<TranslationEntry>();
        var errors = new List<string>();
        TranslationEntry[] entries;
        try { entries = JsonSerializer.Deserialize<TranslationEntry[]>(json) ?? []; }
        catch (JsonException ex) { return new([], [$"JSON inválido: {ex.Message}"], 0); }

        foreach (var entry in entries)
        {
            if (!knownKeys.Contains(entry.Key)) { errors.Add($"Chave desconhecida: {entry.Key}"); continue; }
            if (SupportedLocales.Normalize(entry.LanguageCode) != entry.LanguageCode) { errors.Add($"Idioma inválido: {entry.LanguageCode}"); continue; }
            if (string.IsNullOrWhiteSpace(entry.Value) || UnsafeMarkup().IsMatch(entry.Value)) { errors.Add($"Conteúdo inválido: {entry.Key}"); continue; }
            valid.Add(entry with { IsPublished = false }); // Imports always require explicit review/publication.
        }
        return new(valid, errors, valid.Count(x => existingKeys.Contains($"{x.LanguageCode}:{x.Key}")));
    }
}

public sealed class TranslationExportService
{
    public string ExportJson(IEnumerable<TranslationEntry> entries, string languageCode, string? module = null) =>
        JsonSerializer.Serialize(entries.Where(x => x.LanguageCode == SupportedLocales.Normalize(languageCode) &&
            (module is null || x.Module.Equals(module, StringComparison.OrdinalIgnoreCase))),
            new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Default });
}

public sealed record LocalizedPublicContent(string Route, string LanguageCode, string Title, string Slug, bool IsPublished);

public sealed class HreflangService
{
    public IReadOnlyDictionary<string, Uri> Build(Uri publicBaseUrl, IEnumerable<LocalizedPublicContent> content)
    {
        if (publicBaseUrl.IsLoopback) throw new ArgumentException("PublicBaseUrl não pode apontar para localhost.", nameof(publicBaseUrl));
        return content.Where(x => x.IsPublished && !IsPrivate(x.Route))
            .GroupBy(x => SupportedLocales.Normalize(x.LanguageCode), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => new Uri(publicBaseUrl, $"/{x.Key}/{x.First().Slug.TrimStart('/')}"), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsPrivate(string route) => new[] { "/admin", "/portal", "/partnerportal", "/publicquotes", "/api" }
        .Any(prefix => route.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
