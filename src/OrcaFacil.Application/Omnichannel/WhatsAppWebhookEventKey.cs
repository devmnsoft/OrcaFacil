using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OrcaFacil.Application.Omnichannel;

/// <summary>
/// Chave de deduplicação de um webhook da Cloud API, calculada por <b>conteúdo</b>.
///
/// Duas armadilhas do payload do Meta, ambas corrigidas aqui:
///  1. <c>entry.id</c> é constante para a conta (é o phone number id) — duas mensagens
///     diferentes chegam em POSTs distintos com o mesmo <c>entry.id</c>. Uma chave baseada
///     só nele marcaria a segunda como duplicada e descartaria a mensagem.
///  2. Os callbacks de status de uma mesma mensagem compartilham o mesmo <c>id</c>:
///     <c>sent</c>, <c>delivered</c> e <c>read</c> cairiam na mesma chave e o status
///     nunca avançaria além do primeiro recebido.
/// Por isso a chave usa id de mensagem, e id <b>+ estado</b> de status. Sem ids (payload
/// sem efeito), cai no hash do corpo, determinístico para retentativas do mesmo payload.
/// </summary>
public static class WhatsAppWebhookEventKey
{
    public static string Build(string rawBody)
    {
        var parts = new List<string>();
        string? @object = null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("object", out var o) && o.ValueKind == JsonValueKind.String)
                @object = o.GetString();

            if (root.TryGetProperty("entry", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entries.EnumerateArray())
                {
                    if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (var change in changes.EnumerateArray())
                    {
                        if (!change.TryGetProperty("value", out var value)) continue;
                        CollectParts(value, "messages", withStatus: false, parts);
                        CollectParts(value, "statuses", withStatus: true, parts);
                    }
                }
            }
        }
        catch (JsonException)
        {
            return Hash(rawBody);
        }

        if (parts.Count == 0)
            return Hash(rawBody);

        return $"{@object ?? "whatsapp"}:{string.Join(',', parts.Order(StringComparer.Ordinal))}";
    }

    private static void CollectParts(JsonElement value, string property, bool withStatus, List<string> parts)
    {
        if (!value.TryGetProperty(property, out var items) || items.ValueKind != JsonValueKind.Array) return;
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String) continue;
            var idValue = id.GetString();
            if (string.IsNullOrWhiteSpace(idValue)) continue;

            if (!withStatus) { parts.Add(idValue); continue; }

            var status = item.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String
                ? s.GetString()
                : null;
            parts.Add(string.IsNullOrWhiteSpace(status) ? idValue : $"{idValue}|{status}");
        }
    }

    private static string Hash(string rawBody) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
}
