using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace OrcaFacil.Application.Localization;

public interface ITextLocalizer
{
    string this[string key] { get; }
    string this[string key, params object[] args] { get; }
    string Get(string key, params object[] args);
    string GetForCulture(string key, string cultureCode, params object[] args);
    IReadOnlyDictionary<string, string> GetAll(string? cultureCode = null);
    string CurrentCulture { get; }
}

public interface IMissingKeyTracker
{
    void TrackMissingKey(string key, string cultureCode, string? source = null);
    IReadOnlyCollection<(string Key, string CultureCode, int Count)> GetMissingKeys();
}

public sealed class InMemoryMissingKeyTracker : IMissingKeyTracker
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);

    public void TrackMissingKey(string key, string cultureCode, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var composite = $"{SupportedLocales.Normalize(cultureCode)}:{key.Trim()}";
        _counts.AddOrUpdate(composite, 1, (_, current) => current + 1);
    }

    public IReadOnlyCollection<(string Key, string CultureCode, int Count)> GetMissingKeys()
    {
        return _counts.Select(kv =>
        {
            var parts = kv.Key.Split(':', 2);
            return (parts.Length > 1 ? parts[1] : parts[0], parts[0], kv.Value);
        }).ToList();
    }
}

public sealed class JsonTextLocalizer : ITextLocalizer
{
    private readonly IMissingKeyTracker _missingTracker;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _catalogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly string? _baseDirectory;

    public JsonTextLocalizer(IMissingKeyTracker? missingTracker = null, string? baseDirectory = null)
    {
        _missingTracker = missingTracker ?? new InMemoryMissingKeyTracker();
        _baseDirectory = baseDirectory;
        PreloadCatalogs();
    }

    public string CurrentCulture => SupportedLocales.Normalize(CultureInfo.CurrentUICulture.Name);

    public string this[string key] => Get(key);

    public string this[string key, params object[] args] => Get(key, args);

    public string Get(string key, params object[] args)
    {
        return GetForCulture(key, CurrentCulture, args);
    }

    public string GetForCulture(string key, string cultureCode, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        var targetCulture = SupportedLocales.Normalize(cultureCode);
        var catalog = GetOrLoadCatalog(targetCulture);

        if (!catalog.TryGetValue(key, out var template) || string.IsNullOrWhiteSpace(template))
        {
            // Fallback para pt-BR
            var fallbackCatalog = GetOrLoadCatalog(SupportedLocales.Default);
            if (!fallbackCatalog.TryGetValue(key, out template) || string.IsNullOrWhiteSpace(template))
            {
                _missingTracker.TrackMissingKey(key, targetCulture);
                return key;
            }
        }

        if (args is { Length: > 0 })
        {
            try
            {
                var cultureInfo = CultureInfo.GetCultureInfo(targetCulture);
                return string.Format(cultureInfo, template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        return template;
    }

    public IReadOnlyDictionary<string, string> GetAll(string? cultureCode = null)
    {
        var target = SupportedLocales.Normalize(cultureCode ?? CurrentCulture);
        return GetOrLoadCatalog(target);
    }

    private void PreloadCatalogs()
    {
        foreach (var code in SupportedLocales.All.Keys)
        {
            GetOrLoadCatalog(code);
        }
    }

    private IReadOnlyDictionary<string, string> GetOrLoadCatalog(string cultureCode)
    {
        var normalized = SupportedLocales.Normalize(cultureCode);
        return _catalogs.GetOrAdd(normalized, LoadCatalogFromDisk);
    }

    private IReadOnlyDictionary<string, string> LoadCatalogFromDisk(string cultureCode)
    {
        var fileName = $"resources.{cultureCode}.json";
        var candidatePaths = new List<string>();

        if (!string.IsNullOrWhiteSpace(_baseDirectory))
        {
            candidatePaths.Add(Path.Combine(_baseDirectory, fileName));
            candidatePaths.Add(Path.Combine(_baseDirectory, "Localization", fileName));
        }

        var appBase = AppDomain.CurrentDomain.BaseDirectory;
        candidatePaths.Add(Path.Combine(appBase, "Localization", fileName));
        candidatePaths.Add(Path.Combine(appBase, fileName));

        // Procura em diretórios relativos para desenvolvimento e testes
        var currentDir = Directory.GetCurrentDirectory();
        candidatePaths.Add(Path.Combine(currentDir, "Localization", fileName));
        candidatePaths.Add(Path.Combine(currentDir, "src", "OrcaFacil.Web", "Localization", fileName));
        candidatePaths.Add(Path.Combine(currentDir, "..", "src", "OrcaFacil.Web", "Localization", fileName));
        candidatePaths.Add(Path.Combine(currentDir, "..", "..", "src", "OrcaFacil.Web", "Localization", fileName));
        candidatePaths.Add(Path.Combine(currentDir, "..", "..", "..", "src", "OrcaFacil.Web", "Localization", fileName));
        candidatePaths.Add(Path.Combine(currentDir, "..", "..", "..", "..", "src", "OrcaFacil.Web", "Localization", fileName));

        foreach (var path in candidatePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict is not null)
                    {
                        return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                // Ignora caminhos inacessíveis e tenta o próximo
            }
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
