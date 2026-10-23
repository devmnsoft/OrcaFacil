using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Omnichannel;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

/// <summary>
/// Processa webhooks da Cloud API do WhatsApp (Meta). Verifica a assinatura
/// HMAC-SHA256 com o app secret da conta, deduplica por chave de evento e aplica
/// mensagens de entrada e callbacks de status. Nada é simulado: assinatura inválida
/// ou número desconhecido são rejeitados.
/// </summary>
public sealed class WhatsAppWebhookProcessor(
    OrcaFacilDbContext db,
    IDataProtectionProvider protection,
    ILogger<WhatsAppWebhookProcessor> logger) : IWhatsAppWebhookProcessor
{
    private readonly IDataProtector _protector = protection.CreateProtector("OrcaFacil.Omnichannel.WhatsApp.v1");

    public async Task<(bool Ok, string? Challenge)> VerifyHandshakeAsync(
        string? mode, string? token, string? challenge, CancellationToken ct = default)
    {
        if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase)) return (false, null);
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(challenge)) return (false, null);
        var known = await db.OmnichannelWhatsAppAccounts.AsNoTracking()
            .AnyAsync(x => x.WebhookVerifyToken == token && x.IsEnabled && x.IsDeleted == false, ct);
        return known ? (true, challenge) : (false, null);
    }

    public async Task<WhatsAppWebhookResult> ProcessAsync(
        string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        var signature = Header(headers, WhatsAppSignature.HeaderName);
        // Chave por conteúdo (mensagem/status), não por entry.id: ver WhatsAppWebhookEventKey.
        var eventKey = WhatsAppWebhookEventKey.Build(rawBody);
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            var phoneNumberId = FindRecipientPhoneNumberId(root);
            var account = phoneNumberId is null
                ? null
                : await db.OmnichannelWhatsAppAccounts.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PhoneNumberId == phoneNumberId && x.IsEnabled && x.IsDeleted == false, ct);

            if (account is null)
                return new(false, false, eventKey, "unauthorized", "Número de telefone não pertence a nenhuma conta habilitada.");

            if (!WhatsAppSignature.Verify(Decrypt(account.ProtectedAppSecret), rawBody, signature))
                return new(false, false, eventKey, "unauthorized", "Assinatura do webhook inválida.");

            // event_type é varchar(40): a chave por conteúdo (object + ids do Meta) não
            // cabe nela. O hash curto deduplica; a chave completa fica em provider_reference
            // (varchar 255) para diagnóstico.
            var dedupColumn = DedupColumn(eventKey);
            if (await db.OmnichannelDeliveryLogs.AnyAsync(
                    x => x.EventType == dedupColumn && x.AccountId == account.AccountId, ct))
                return new(true, true, eventKey, "duplicate", "Evento já processado.");

            var channelId = await EnsureChannelAsync(account.AccountId, account.PhoneNumberId, ct);
            var applied = await ApplyEntriesAsync(account.AccountId, channelId, root, ct);

            db.OmnichannelDeliveryLogs.Add(new OmnichannelDeliveryLog
            {
                AccountId = account.AccountId,
                EventType = dedupColumn,
                ProviderReference = eventKey.Length <= 255 ? eventKey : eventKey[..255],
                SanitizedError = null
            });
            await db.SaveChangesAsync(ct);

            return new(true, false, eventKey, applied ? "applied" : "no_effect", applied ? null : "Nenhuma mensagem ou status aplicável.");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "WHATSAPP_WEBHOOK_MALFORMED");
            return new(false, false, eventKey, "malformed", "Corpo do webhook inválido.");
        }
    }

    private async Task<bool> ApplyEntriesAsync(Guid accountId, Guid channelId, JsonElement root, CancellationToken ct)
    {
        if (!root.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return false;

        var applied = false;
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;

                if (value.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
                {
                    foreach (var message in messages.EnumerateArray())
                        applied |= await ApplyInboundAsync(accountId, channelId, value, message, ct);
                }

                if (value.TryGetProperty("statuses", out var statuses) && statuses.ValueKind == JsonValueKind.Array)
                {
                    foreach (var status in statuses.EnumerateArray())
                        applied |= await ApplyStatusAsync(accountId, status, ct);
                }
            }
        }
        return applied;
    }

    private async Task<bool> ApplyInboundAsync(Guid accountId, Guid channelId, JsonElement metadata, JsonElement message, CancellationToken ct)
    {
        var externalId = message.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(externalId)) return false;

        var existing = await db.OmnichannelMessages.AsNoTracking()
            .AnyAsync(x => x.AccountId == accountId && x.ExternalMessageId == externalId, ct);
        if (existing) return false;

        var from = message.TryGetProperty("from", out var fromEl) && fromEl.ValueKind == JsonValueKind.String ? fromEl.GetString() ?? "" : "";
        var text = ExtractText(message);
        var timestamp = message.TryGetProperty("timestamp", out var ts) && ts.TryGetInt64(out var epoch)
            ? DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime
            : DateTime.UtcNow;

        var conversation = await FindOrCreateConversationAsync(accountId, channelId, from, ct);
        db.OmnichannelMessages.Add(new OmnichannelMessage
        {
            AccountId = accountId,
            ConversationId = conversation.Id,
            ChannelId = channelId,
            SenderType = "WhatsAppContact",
            SenderDisplayName = from,
            Direction = "Inbound",
            Type = OmnichannelMessageType.Inbound,
            Status = OmnichannelMessageStatus.Received,
            // Mesma disciplina da caixa de entrada do OmnichannelService: o conteúdo que
            // chega do provedor é texto do contato e é gravado codificado (o provedor é a
            // fronteira de confiança; o HTML nunca vem dele).
            Content = WebUtility.HtmlEncode(text),
            ExternalMessageId = externalId
        });
        conversation.LastMessageAt = timestamp;
        conversation.Touch();
        return true;
    }

    private async Task<bool> ApplyStatusAsync(Guid accountId, JsonElement status, CancellationToken ct)
    {
        var externalId = status.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(externalId)) return false;

        var message = await db.OmnichannelMessages
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ExternalMessageId == externalId, ct);
        if (message is null) return false;

        var state = status.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
        var timestamp = status.TryGetProperty("timestamp", out var ts) && ts.TryGetInt64(out var epoch)
            ? DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime
            : DateTime.UtcNow;

        switch (state)
        {
            case "sent":
                message.Status = OmnichannelMessageStatus.Sent;
                message.SentAt ??= timestamp;
                break;
            case "delivered":
                message.Status = OmnichannelMessageStatus.Delivered;
                message.DeliveredAt ??= timestamp;
                break;
            case "read":
                message.Status = OmnichannelMessageStatus.Read;
                message.ReadAt ??= timestamp;
                break;
            case "failed":
                message.Status = OmnichannelMessageStatus.Failed;
                message.FailedAt ??= timestamp;
                break;
            default:
                return false;
        }
        message.Touch();
        return true;
    }

    private async Task<OmnichannelConversation> FindOrCreateConversationAsync(Guid accountId, Guid channelId, string from, CancellationToken ct)
    {
        var conversation = await db.OmnichannelConversations
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ChannelId == channelId && x.Subject == $"WhatsApp {from}", ct);
        if (conversation is not null) return conversation;

        conversation = new OmnichannelConversation
        {
            AccountId = accountId,
            ChannelId = channelId,
            Subject = $"WhatsApp {from}",
            Status = OmnichannelConversationStatus.Open,
            LastMessageAt = DateTime.UtcNow
        };
        db.OmnichannelConversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation;
    }

    private async Task<Guid> EnsureChannelAsync(Guid accountId, string phoneNumberId, CancellationToken ct)
    {
        var channel = await db.OmnichannelChannels
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.Type == OmnichannelChannelType.WhatsAppApi && x.IsDeleted == false, ct);
        if (channel is not null) return channel.Id;

        channel = new OmnichannelChannel
        {
            AccountId = accountId,
            Type = OmnichannelChannelType.WhatsAppApi,
            Status = OmnichannelChannelStatus.Configured,
            Name = $"WhatsApp {phoneNumberId}",
            IsEnabled = true
        };
        db.OmnichannelChannels.Add(channel);
        await db.SaveChangesAsync(ct);
        return channel.Id;
    }

    private static string ExtractText(JsonElement message)
    {
        if (message.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.Object &&
            text.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String)
            return body.GetString();

        // Mensagem sem texto (mídia, localização, botão...): registra o tipo real recebido
        // em vez de gravar uma linha vazia na caixa de entrada.
        var type = message.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        return string.IsNullOrWhiteSpace(type) ? "[mensagem sem texto]" : $"[{type}]";
    }

    private static string? FindRecipientPhoneNumberId(JsonElement root)
    {
        if (!root.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array) return null;
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array) continue;
            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;
                if (value.TryGetProperty("metadata", out var metadata) &&
                    metadata.TryGetProperty("phone_number_id", out var phone) &&
                    phone.ValueKind == JsonValueKind.String)
                    return phone.GetString();
            }
        }
        return null;
    }

    /// <summary>Coluna de deduplicação: prefixo fixo + hash curto da chave de conteúdo
    /// (cabe em event_type varchar(40); a chave completa vai em provider_reference).</summary>
    private static string DedupColumn(string eventKey)
    {
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(eventKey)));
        return $"whatsapp.webhook:{hash[..16].ToLowerInvariant()}";
    }

    private string Decrypt(string? protectedValue)
        => string.IsNullOrWhiteSpace(protectedValue) ? string.Empty : _protector.Unprotect(protectedValue);

    private static string? Header(IReadOnlyDictionary<string, string> headers, string name)
        => headers.TryGetValue(name, out var value) ? value
            : headers.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
}
