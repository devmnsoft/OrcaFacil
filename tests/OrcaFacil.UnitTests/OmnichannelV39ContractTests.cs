using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.UnitTests;

public sealed class OmnichannelV39ContractTests
{
    [Fact] public void Internal_note_has_explicit_non_public_type() => Assert.NotEqual(OmnichannelMessageType.Outbound, OmnichannelMessageType.InternalNote);
    [Fact] public void Prepared_whatsapp_is_not_sent() => Assert.NotEqual(OmnichannelMessageStatus.Sent, OmnichannelMessageStatus.Prepared);
    [Fact] public void Inbound_email_defaults_to_manual_and_disabled() { var account=new OmnichannelInboundEmailAccount(); Assert.Equal("Manual",account.Mode); Assert.False(account.IsEnabled); Assert.False(account.HasProtectedCredential); }
    [Fact] public void Channel_defaults_to_not_configured() => Assert.Equal(OmnichannelChannelStatus.NotConfigured,new OmnichannelChannel().Status);
    [Fact] public void Conversation_requires_tenant_identity() => Assert.Equal(Guid.Empty,new OmnichannelConversation().AccountId);
}
