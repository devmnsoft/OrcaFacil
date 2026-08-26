using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

/// <summary>Tenant-scoped inbox. Provider delivery is recorded only after a real provider callback.</summary>
public sealed class OmnichannelService(OrcaFacilDbContext db)
{
    public Task<List<OmnichannelConversation>> InboxAsync(Guid accountId, int page, int pageSize, CancellationToken ct = default) =>
        db.OmnichannelConversations.AsNoTracking().Where(x=>x.AccountId==accountId && x.Status!=OmnichannelConversationStatus.Spam && x.Status!=OmnichannelConversationStatus.Archived)
            .OrderByDescending(x=>x.LastMessageAt ?? x.CreatedAt).Skip(Math.Max(0,page-1)*Math.Clamp(pageSize,1,100)).Take(Math.Clamp(pageSize,1,100)).ToListAsync(ct);

    public async Task<List<OmnichannelMessage>> MessagesAsync(Guid accountId, Guid conversationId, bool includeInternal, CancellationToken ct = default)
    {
        if (!await db.OmnichannelConversations.AnyAsync(x=>x.Id==conversationId && x.AccountId==accountId,ct)) return [];
        return await db.OmnichannelMessages.AsNoTracking().Where(x=>x.AccountId==accountId && x.ConversationId==conversationId && (includeInternal || x.Type!=OmnichannelMessageType.InternalNote)).OrderBy(x=>x.CreatedAt).ToListAsync(ct);
    }

    public async Task<OmnichannelMessage> AddMessageAsync(Guid accountId, Guid conversationId, Guid channelId, string sender, string content, OmnichannelMessageType type, CancellationToken ct = default)
    {
        var conversation=await db.OmnichannelConversations.SingleOrDefaultAsync(x=>x.Id==conversationId && x.AccountId==accountId,ct) ?? throw new KeyNotFoundException("Conversa não encontrada nesta conta.");
        var channel=await db.OmnichannelChannels.SingleOrDefaultAsync(x=>x.Id==channelId && (x.AccountId==accountId || x.IsGlobal),ct) ?? throw new InvalidOperationException("Canal indisponível para esta conta.");
        var outbound=type is OmnichannelMessageType.Outbound;
        var canDispatch=channel.IsEnabled && channel.Status is OmnichannelChannelStatus.Configured or OmnichannelChannelStatus.Healthy;
        var message=new OmnichannelMessage { AccountId=accountId,ConversationId=conversationId,ChannelId=channelId,SenderType="InternalUser",SenderDisplayName=sender.Trim(),Direction=outbound?"Outbound":"Internal",Type=outbound&&!canDispatch?OmnichannelMessageType.Draft:type,Status=outbound&&!canDispatch?OmnichannelMessageStatus.Draft:outbound?OmnichannelMessageStatus.Queued:OmnichannelMessageStatus.Received,Content=WebUtility.HtmlEncode(content.Trim()) };
        db.OmnichannelMessages.Add(message); conversation.LastMessageAt=DateTime.UtcNow; conversation.Touch(); await db.SaveChangesAsync(ct); return message;
    }

    public async Task<bool> RecordSlaBreachOnceAsync(Guid accountId, Guid conversationId, string type, CancellationToken ct=default)
    {
        if (!await db.OmnichannelConversations.AnyAsync(x=>x.Id==conversationId&&x.AccountId==accountId,ct)) throw new KeyNotFoundException();
        if (await db.OmnichannelSlaEvents.AnyAsync(x=>x.ConversationId==conversationId&&x.Type==type,ct)) return false;
        db.OmnichannelSlaEvents.Add(new(){AccountId=accountId,ConversationId=conversationId,Type=type}); await db.SaveChangesAsync(ct); return true;
    }
}

public sealed record WebChatStartResult(Guid ConversationId, string SessionToken, string Acknowledgement);
public sealed class WebChatSessionService(OrcaFacilDbContext db)
{
    public async Task<WebChatStartResult> StartAsync(Guid accountId, string name, string email, string message, bool consent, CancellationToken ct=default)
    {
        if (!consent) throw new InvalidOperationException("O consentimento configurado é obrigatório.");
        var channel=await db.OmnichannelChannels.SingleOrDefaultAsync(x=>x.AccountId==accountId&&x.Type==OmnichannelChannelType.WebChat&&x.IsEnabled&&x.Status==OmnichannelChannelStatus.Healthy,ct) ?? throw new InvalidOperationException("Chat Web não está configurado e saudável.");
        var conversation=new OmnichannelConversation{AccountId=accountId,ChannelId=channel.Id,Subject=$"Chat de {name.Trim()}",LastMessageAt=DateTime.UtcNow}; db.OmnichannelConversations.Add(conversation);
        db.OmnichannelMessages.Add(new(){AccountId=accountId,ConversationId=conversation.Id,ChannelId=channel.Id,SenderType="PublicLead",SenderDisplayName=name.Trim(),Direction="Inbound",Type=OmnichannelMessageType.Inbound,Status=OmnichannelMessageStatus.Received,Content=WebUtility.HtmlEncode(message.Trim())});
        var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        db.OmnichannelWebChatSessions.Add(new(){AccountId=accountId,ConversationId=conversation.Id,TokenHash=Hash(token),VisitorName=name.Trim(),VisitorEmail=email.Trim().ToLowerInvariant(),ExpiresAt=DateTime.UtcNow.AddDays(30),ConsentAccepted=true});
        await db.SaveChangesAsync(ct); return new(conversation.Id,token,"Recebemos sua mensagem. Nossa equipe responderá assim que possível.");
    }
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed class InboundEmailService(OrcaFacilDbContext db)
{
    public async Task<OmnichannelChannelStatus> StatusAsync(Guid accountId, CancellationToken ct=default)
    {
        var configured=await db.OmnichannelInboundEmailAccounts.AnyAsync(x=>x.AccountId==accountId&&x.IsEnabled&&x.HasProtectedCredential&&x.Mode!="Manual",ct);
        return configured?OmnichannelChannelStatus.Configured:OmnichannelChannelStatus.NotConfigured;
    }
}

public sealed record PreparedWhatsAppMessage(string Uri, OmnichannelMessageStatus Status);
public sealed class OmnichannelWhatsAppService
{
    public PreparedWhatsAppMessage Prepare(string phone, string content) => new($"https://wa.me/{Uri.EscapeDataString(new string(phone.Where(char.IsDigit).ToArray()))}?text={Uri.EscapeDataString(content)}",OmnichannelMessageStatus.Prepared);
}

public sealed class OmnichannelOptOutService(OrcaFacilDbContext db)
{
    public Task<bool> CanSendCommercialAsync(Guid accountId,string identityHash,string channel,CancellationToken ct=default)=>db.OmnichannelOptOutEvents.AllAsync(x=>x.AccountId!=accountId||x.IdentityHash!=identityHash||x.Channel!=channel||x.Scope!="Commercial",ct);
}
