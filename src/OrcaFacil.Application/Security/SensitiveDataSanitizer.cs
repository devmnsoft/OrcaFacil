using System.Text.Json;
using System.Text.RegularExpressions;

namespace OrcaFacil.Application.Security;

public interface ISensitiveDataSanitizer
{
    string Sanitize(string? value);
    string SanitizeJson(object? value);
}

public sealed partial class SensitiveDataSanitizer : ISensitiveDataSanitizer
{
    private const string Redacted = "[REDACTED]";

    public string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
        var sanitized = ConnectionStringRegex().Replace(value, "$1=" + Redacted);
        return SecretPairRegex().Replace(sanitized, "$1$2" + Redacted);
    }

    public string SanitizeJson(object? value)
    {
        if (value is null) return "{}";
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteSanitized(writer, document.RootElement, null);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteSanitized(Utf8JsonWriter writer, JsonElement element, string? propertyName)
    {
        if (propertyName is not null && IsSecret(propertyName)) { writer.WriteStringValue(Redacted); return; }
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()) { writer.WritePropertyName(property.Name); WriteSanitized(writer, property.Value, property.Name); }
                writer.WriteEndObject(); break;
            case JsonValueKind.Array:
                writer.WriteStartArray(); foreach (var item in element.EnumerateArray()) WriteSanitized(writer, item, null); writer.WriteEndArray(); break;
            default: element.WriteTo(writer); break;
        }
    }

    private static bool IsSecret(string name)
    {
        var normalized = name.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        return normalized.Contains("password") || normalized.Contains("passwordhash") || normalized.Contains("token") ||
               normalized.Contains("apikey") || normalized.Contains("secret") || normalized.Contains("connectionstring") ||
               normalized.Contains("smtpcredential") || normalized.Contains("pepper") || normalized.Contains("cookie");
    }

    [GeneratedRegex(@"(?i)\b(password|passwordhash|token|api[_-]?key|secret|pepper|cookie)(\s*[:=]\s*)[^\s,;}&]+")]
    private static partial Regex SecretPairRegex();

    [GeneratedRegex(@"(?i)\b(Host|Database|Username|User Id|Password)\s*=\s*[^;]+")]
    private static partial Regex ConnectionStringRegex();
}
