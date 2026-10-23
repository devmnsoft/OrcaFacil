using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

/// <summary>
/// Conta da Cloud API do WhatsApp (Meta). As credenciais são armazenadas
/// protegidas (criptografia em repouso) e o segredo do app é usado para
/// verificar a assinatura HMAC dos webhooks. Um registro por conta.
/// </summary>
public sealed class OmnichannelWhatsAppAccount : Entity
{
    public Guid AccountId { get; set; }

    /// <summary>Phone number id da Meta (para onde a Cloud API responde).</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    public string? BusinessAccountId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Access token de sistema protegido (somente em repouso).</summary>
    public string? ProtectedAccessToken { get; set; }

    /// <summary>App secret protegido, usado no HMAC-SHA256 dos webhooks.</summary>
    public string? ProtectedAppSecret { get; set; }

    /// <summary>Token de verificação do handshake GET (hub.verify_token).</summary>
    public string? WebhookVerifyToken { get; set; }

    public bool IsEnabled { get; set; }

    /// <summary>NotConfigured | Configured | Healthy | Degraded | Failed | Disabled.</summary>
    public string Status { get; set; } = "NotConfigured";

    public DateTime? LastHealthCheckAt { get; set; }

    public string? LastFailure { get; set; }
}
