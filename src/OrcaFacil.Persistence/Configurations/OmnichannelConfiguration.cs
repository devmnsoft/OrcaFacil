using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class OmnichannelConfiguration :
    IEntityTypeConfiguration<OmnichannelChannel>, IEntityTypeConfiguration<OmnichannelConversation>,
    IEntityTypeConfiguration<OmnichannelParticipant>, IEntityTypeConfiguration<OmnichannelMessage>,
    IEntityTypeConfiguration<OmnichannelDeliveryLog>, IEntityTypeConfiguration<OmnichannelWebChatSession>,
    IEntityTypeConfiguration<OmnichannelInboundEmailAccount>, IEntityTypeConfiguration<OmnichannelOptOutEvent>,
    IEntityTypeConfiguration<OmnichannelSlaEvent>, IEntityTypeConfiguration<OmnichannelCsatResponse>
{
    private static void Base<T>(EntityTypeBuilder<T> b, string table) where T : Entity { b.ToTable(table,"orcafacil"); b.ConfigureBase(); b.HasQueryFilter(x=>!x.IsDeleted); }
    public void Configure(EntityTypeBuilder<OmnichannelChannel> b) { Base(b,"omnichannel_channels"); b.Property(x=>x.Type).HasConversion<string>().HasMaxLength(32); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Name).HasMaxLength(120).IsRequired(); b.HasIndex(x=>new{x.AccountId,x.Type}).IsUnique(); }
    public void Configure(EntityTypeBuilder<OmnichannelConversation> b) { Base(b,"omnichannel_conversations"); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Subject).HasMaxLength(200); b.HasIndex(x=>new{x.AccountId,x.Status,x.LastMessageAt}); }
    public void Configure(EntityTypeBuilder<OmnichannelParticipant> b) { Base(b,"omnichannel_conversation_participants"); b.Property(x=>x.Type).HasMaxLength(32); b.Property(x=>x.DisplayName).HasMaxLength(160); b.HasIndex(x=>new{x.AccountId,x.ConversationId,x.Type,x.ReferenceId}).IsUnique(); }
    public void Configure(EntityTypeBuilder<OmnichannelMessage> b) { Base(b,"omnichannel_messages"); b.Property(x=>x.Type).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Content).HasMaxLength(20000).IsRequired(); b.HasIndex(x=>new{x.AccountId,x.ConversationId,x.CreatedAt}); b.HasIndex(x=>new{x.AccountId,x.ChannelId,x.ExternalMessageId}).IsUnique(); }
    public void Configure(EntityTypeBuilder<OmnichannelDeliveryLog> b) { Base(b,"omnichannel_message_delivery_logs"); b.HasIndex(x=>new{x.AccountId,x.MessageId,x.CreatedAt}); }
    public void Configure(EntityTypeBuilder<OmnichannelWebChatSession> b) { Base(b,"omnichannel_web_chat_sessions"); b.Property(x=>x.TokenHash).HasMaxLength(64); b.HasIndex(x=>x.TokenHash).IsUnique(); }
    public void Configure(EntityTypeBuilder<OmnichannelInboundEmailAccount> b) { Base(b,"omnichannel_inbound_email_accounts"); b.HasIndex(x=>new{x.AccountId,x.Address}).IsUnique(); }
    public void Configure(EntityTypeBuilder<OmnichannelOptOutEvent> b) { Base(b,"omnichannel_opt_out_events"); b.HasIndex(x=>new{x.AccountId,x.IdentityHash,x.Channel,x.Scope}); }
    public void Configure(EntityTypeBuilder<OmnichannelSlaEvent> b) { Base(b,"omnichannel_sla_events"); b.HasIndex(x=>new{x.ConversationId,x.Type}).IsUnique(); }
    public void Configure(EntityTypeBuilder<OmnichannelCsatResponse> b) { Base(b,"omnichannel_csat_responses"); b.HasIndex(x=>x.RequestTokenHash).IsUnique(); b.HasIndex(x=>x.ConversationId).IsUnique(); }
}
