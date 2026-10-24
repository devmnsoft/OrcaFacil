namespace OrcaFacil.Application.Omnichannel;

/// <summary>Resultado de um envio à Cloud API do WhatsApp.</summary>
public sealed record WhatsAppSendResult(bool Succeeded, string? ExternalMessageId, string? Error);

/// <summary>Cliente da Cloud API (Meta). Falha de rede/provedor é retornada como
/// resultado, nunca simulada como sucesso.</summary>
public interface IWhatsAppGraphClient
{
    Task<WhatsAppSendResult> SendTextMessageAsync(
        string accessToken, string phoneNumberId, string toPhone, string content, CancellationToken ct = default);
}

/// <summary>Resultado do processamento de um webhook do WhatsApp.</summary>
public sealed record WhatsAppWebhookResult(
    bool Succeeded, bool Duplicate, string? EventKey, string Status, string? Message);

/// <summary>Processa webhooks da Cloud API: verifica assinatura HMAC, deduplica e
/// aplica mensagens de entrada e callbacks de status.</summary>
public interface IWhatsAppWebhookProcessor
{
    Task<WhatsAppWebhookResult> ProcessAsync(
        string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);

    /// <summary>Handshake GET do Meta (hub.mode/verify_token/challenge).</summary>
    Task<(bool Ok, string? Challenge)> VerifyHandshakeAsync(string? mode, string? token, string? challenge, CancellationToken ct = default);
}
