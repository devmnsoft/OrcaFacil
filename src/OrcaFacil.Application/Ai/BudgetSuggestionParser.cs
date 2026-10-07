using System.Text.Json;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Ai;

public sealed record ParsedBudgetItem(Guid? CatalogItemId, string Description, decimal Quantity);
public sealed record ParsedBudgetSuggestion(bool Accepted, string Scope, string Notes, IReadOnlyList<ParsedBudgetItem> Items, IReadOnlyList<string> Questions);

public static class BudgetSuggestionParser
{
    public const int MaxItems = 30;

    public static ParsedBudgetSuggestion Parse(string? content, IReadOnlyDictionary<Guid, ServiceCatalogItem> catalog)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new(false, string.Empty, string.Empty, [], []);
        var json = ExtractObject(content);
        if (json is null) return new(false, string.Empty, string.Empty, [], []);
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new(false, string.Empty, string.Empty, [], []);
            var root = document.RootElement;
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > MaxItems)
                return new(false, string.Empty, string.Empty, [], ["A resposta excedeu o limite de itens e não foi aplicada."]);
            if (!TryRead(root, "scope", 2000, out var scope) || !TryRead(root, "notes", 2000, out var notes))
                return new(false, string.Empty, string.Empty, [], ["A resposta excedeu o tamanho permitido e não foi aplicada."]);
            var questions = ReadStrings(root, "questions", 8, 300);
            var parsed = new List<ParsedBudgetItem>();
            if (root.TryGetProperty("items", out items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    Guid? catalogId = null;
                    if (item.TryGetProperty("catalogItemId", out var idElement) && idElement.ValueKind == JsonValueKind.String && Guid.TryParse(idElement.GetString(), out var id))
                    {
                        if (!catalog.ContainsKey(id)) continue;
                        catalogId = id;
                    }
                    var quantity = 1m;
                    if (item.TryGetProperty("quantity", out var quantityElement) && quantityElement.TryGetDecimal(out var value))
                    {
                        if (value <= 0 || value > 10000) continue;
                        quantity = value;
                    }
                    if (!TryRead(item, "description", 500, out var description)) continue;
                    if (catalogId is null && string.IsNullOrWhiteSpace(description)) continue;
                    parsed.Add(new(catalogId, description, quantity));
                }
            }
            return new(true, scope, notes, parsed, questions);
        }
        catch (JsonException)
        {
            return new(false, string.Empty, string.Empty, [], []);
        }
    }

    private static string? ExtractObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        return content[start..(end + 1)];
    }

    private static bool TryRead(JsonElement element, string name, int max, out string text)
    {
        text = string.Empty;
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String) return true;
        text = value.GetString()?.Trim() ?? string.Empty;
        return text.Length <= max;
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement element, string name, int maxItems, int maxLength)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array) return [];
        return value.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()?.Trim() ?? string.Empty)
            .Where(x => x.Length > 0)
            .Take(maxItems)
            .Select(x => x.Length <= maxLength ? x : x[..maxLength])
            .ToArray();
    }
}
