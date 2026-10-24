using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Omnichannel;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

/// <summary>
/// Envio oficial via Cloud API do WhatsApp. Carrega a conta habilitada, descriptografa
/// o token, respeita opt-out e registra a mensagem (Queued até o callback do provedor,
/// Failed quando o envio falha). Nenhum envio é simulado.
/// </summary>
public sealed class OmnichannelWhatsAppApiService(
    OrcaFacilDbContext db,
    IDataProtectionProvider protection,
    IWhatsAppGraphClient graphClient,
    ILogger<OmnichannelWhatsAppApiService> logger)
{
    private readonly IDataProtector _protector = protection.CreateProtector("OrcaFacil.Omnichannel.WhatsApp.v1");

    public async Task<WhatsAppSendResult> SendAsync(
        Guid accountId, string toPhone, string content, Guid? userId, CancellationToken ct = default)
    {
        // Tracking ligado de propósito: a conta é atualizada com o resultado do envio
        // (Status/LastFailure/LastHealthCheckAt) no mesmo SaveChanges da mensagem.
        var account = await db.OmnichannelWhatsAppAccounts
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.IsEnabled && x.IsDeleted == false, ct);
        if (account is null)
            return new WhatsAppSendResult(false, null, "Conta WhatsApp não configurada ou desabilitada.");
        if (string.IsNullOrWhiteSpace(account.ProtectedAccessToken))
            return new WhatsAppSendResult(false, null, "Access token ausente na conta WhatsApp.");

        var phone = WhatsAppSignature.NormalizePhone(toPhone);
        if (string.IsNullOrWhiteSpace(phone))
            return new WhatsAppSendResult(false, null, "Telefone de destino inválido.");

        var identityHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(phone))).ToLowerInvariant();
        if (!await db.OmnichannelOptOutEvents.AllAsync(
                x => x.AccountId != accountId || x.IdentityHash != identityHash || x.Channel != "WhatsApp" || x.Scope != "Commercial", ct))
            return new WhatsAppSendResult(false, null, "Destinatário optou por não receber mensagens comerciais.");

        var token = _protector.Unprotect(account.ProtectedAccessToken);
        var result = await graphClient.SendTextMessageAsync(token, account.PhoneNumberId, phone, content, ct);

        var channelId = await EnsureChannelAsync(accountId, account.PhoneNumberId, ct);
        var conversation = await FindOrCreateConversationAsync(accountId, channelId, phone, ct);
        db.OmnichannelMessages.Add(new OmnichannelMessage
        {
            AccountId = accountId,
            ConversationId = conversation.Id,
            ChannelId = channelId,
            SenderType = "InternalUser",
            SenderUserId = userId,
            SenderDisplayName = account.DisplayName,
            Direction = "Outbound",
            Type = OmnichannelMessageType.Outbound,
            Status = result.Succeeded ? OmnichannelMessageStatus.Queued : OmnichannelMessageStatus.Failed,
            // O texto vai cru para a Cloud API; o registro na caixa de entrada segue a
            // mesma disciplina do OmnichannelService (conteúdo codificado).
            Content = WebUtility.HtmlEncode(content),
            ExternalMessageId = result.ExternalMessageId,
            FailedAt = result.Succeeded ? null : DateTime.UtcNow
        });
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.Touch();

        if (result.Succeeded)
        {
            account.Status = OmnichannelChannelStatus.Healthy.ToString();
            account.LastFailure = null;
            account.LastHealthCheckAt = DateTime.UtcNow;
        }
        else
        {
            account.Status = OmnichannelChannelStatus.Failed.ToString();
            account.LastFailure = result.Error;
            db.OmnichannelDeliveryLogs.Add(new OmnichannelDeliveryLog
            {
                AccountId = accountId,
                EventType = "whatsapp.send.failed",
                SanitizedError = result.Error
            });
        }
        account.Touch();
        await db.SaveChangesAsync(ct);

        logger.LogInformation("WHATSAPP_SEND Recorded Account {AccountId} Succeeded {Succeeded}", accountId, result.Succeeded);
        return result;
    }

    private async Task<OmnichannelConversation> FindOrCreateConversationAsync(Guid accountId, Guid channelId, string phone, CancellationToken ct)
    {
        var conversation = await db.OmnichannelConversations
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ChannelId == channelId && x.Subject == $"WhatsApp {phone}", ct);
        if (conversation is not null) return conversation;

        conversation = new OmnichannelConversation
        {
            AccountId = accountId,
            ChannelId = channelId,
            Subject = $"WhatsApp {phone}",
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
}
