using System.Text.Json;

namespace OrcaFacil.Application.Ai;

public static class CommercialReviewParser
{
    private static readonly IReadOnlyDictionary<string, string> SeverityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["critica"] = "Crítica", ["critico"] = "Crítica", ["critical"] = "Crítica",
        ["alta"] = "Alta", ["high"] = "Alta",
        ["media"] = "Média", ["média"] = "Média", ["medium"] = "Média", ["normal"] = "Média",
        ["baixa"] = "Baixa", ["low"] = "Baixa", ["info"] = "Baixa", ["sugestao"] = "Baixa", ["sugestão"] = "Baixa",
    };

    /// <summary>Extrai e valida o JSON esperado do provedor. Campos ausentes ou inválidos são descartados, não inventados.</summary>
    public static CommercialReviewParsed Parse(string? content)
    {
        var json = ExtractJson(content);
        if (json is null) return new(string.Empty, string.Empty, []);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = Clean(root, "title", 120);
            var conditions = Clean(root, "conditions", 1000);

            var findings = new List<CommercialReviewFinding>();
            if (root.TryGetProperty("findings", out var array) && array.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in array.EnumerateArray())
                {
                    if (findings.Count >= 8) break;
                    if (element.ValueKind != JsonValueKind.Object) continue;
                    var message = Clean(element, "message", 220);
                    if (string.IsNullOrWhiteSpace(message)) continue;
                    var rawSeverity = element.TryGetProperty("severity", out var sev) && sev.ValueKind == JsonValueKind.String
                        ? sev.GetString() ?? string.Empty : string.Empty;
                    var severity = SeverityMap.TryGetValue(rawSeverity.Trim(), out var mapped) ? mapped : "Média";
                    var category = Clean(element, "category", 40);
                    if (string.IsNullOrWhiteSpace(category)) category = "Revisão";
                    var suggestion = Clean(element, "suggestion", 300);
                    findings.Add(new(category, severity, message!, suggestion ?? string.Empty));
                }
            }

            return new(title ?? string.Empty, conditions ?? string.Empty, findings);
        }
        catch (JsonException)
        {
            return new(string.Empty, string.Empty, []);
        }
    }

    private static string? ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = content.Trim();
        // Remove cercas de código: aceita ```json ... ``` e texto solto ao redor do objeto principal.
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    private static string? Clean(JsonElement parent, string property, int maxLength)
    {
        if (!parent.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String) return null;
        var s = AiRedactionStatic.Sanitize(value.GetString()?.Trim() ?? string.Empty);
        if (s.Length == 0) return null;
        return s.Length > maxLength ? s[..maxLength] : s;
    }
}

/// <summary>Ponto único de redação estático para o parser (evita duplicar instância).</summary>
internal static class AiRedactionStatic
{
    private static readonly AiRedactionService Redaction = new();
    public static string Sanitize(string value) => Redaction.Sanitize(value);
}
