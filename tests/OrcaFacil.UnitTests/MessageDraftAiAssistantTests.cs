using OrcaFacil.Application.Ai;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class MessageDraftAiAssistantTests
{
    private static readonly Guid Account = Guid.NewGuid();

    private static AiRequestContext Context(Guid account = default, bool active = true) =>
        new(account, Guid.NewGuid(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static AiGovernancePolicy Policy(Guid account = default, bool accountActive = true) =>
        new(account, AccountActive: accountActive);

    private readonly MessageDraftAiAssistant _assistant = new(new AiRedactionService());

    [Fact]
    public void Cross_account_request_is_refused_with_empty_draft()
    {
        var result = _assistant.GenerateDraft(Context(Account), Policy(Guid.NewGuid()), "WhatsApp", "FollowUp", "Maria", "ORC-1", 150, null);
        Assert.Empty(result.DraftContent);
        Assert.False(result.Sent);
        Assert.Contains(result.Disclaimers, x => x.Contains("indispon"));
    }

    [Fact]
    public void Inactive_account_is_refused()
    {
        var result = _assistant.GenerateDraft(Context(Account), Policy(Account, accountActive: false), "WhatsApp", "FollowUp", "Maria", "ORC-1", 150, null);
        Assert.Empty(result.DraftContent);
        Assert.False(result.Sent);
    }

    [Fact]
    public void Draft_is_never_sent_and_carries_disclaimer()
    {
        var result = _assistant.GenerateDraft(Context(Account), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-1", 150, null);
        Assert.False(result.Sent);
        Assert.Contains(result.Disclaimers, x => x.Contains("NUNCA"));
    }

    [Fact]
    public void Whatsapp_followup_mentions_client_and_document()
    {
        var result = _assistant.GenerateDraft(Context(Account), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-2026-01", 150, null);
        Assert.Contains("Maria", result.DraftContent);
        Assert.Contains("ORC-2026-01", result.DraftContent);
        Assert.Equal("WhatsApp", result.Channel);
    }

    [Fact]
    public void Email_presentation_is_formal_and_shows_amount()
    {
        var result = _assistant.GenerateDraft(Context(Account), Policy(Account), "Email", "Presentation", "Maria", "ORC-1", 1500, DateTime.UtcNow.Date.AddDays(15));
        Assert.Contains("Prezado", result.DraftContent);
        Assert.Contains("ORC-1", result.DraftContent);
        Assert.Equal("Email", result.Channel);
    }

    [Fact]
    public void Presentation_without_validity_omits_deadline()
    {
        var result = _assistant.GenerateDraft(Context(Account), Policy(Account), "WhatsApp", "Presentation", "Maria", "ORC-1", 150, null);
        Assert.DoesNotContain("válida até", result.DraftContent);
    }

    [Fact]
    public void Customer_name_secrets_are_redacted()
    {
        var result = _assistant.GenerateDraft(Context(Account), Policy(Account), "WhatsApp", "FollowUp", "Maria token=abcdefghijklmnop", "ORC-1", 150, null);
        Assert.DoesNotContain("abcdefghijklmnop", result.DraftContent);
    }
}
