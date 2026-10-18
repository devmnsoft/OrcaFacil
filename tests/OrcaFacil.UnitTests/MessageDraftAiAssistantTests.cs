using OrcaFacil.Application.Ai;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class MessageDraftAiAssistantTests
{
    private static readonly Guid Account = Guid.NewGuid();

    private static AiRequestContext Context(Guid account = default, bool withPermission = false) =>
        new(account, Guid.NewGuid(),
            withPermission
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ai.GenerateDrafts" }
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static AiGovernancePolicy Policy(Guid account = default, bool accountActive = true, bool featureEnabled = true) =>
        new(account, AccountActive: accountActive, FeatureEnabled: featureEnabled);

    private sealed class StubOrchestrator(AiExecutionResult result) : IAiOrchestrator
    {
        public string? LastPurpose { get; private set; }
        public AiRequestContext? LastContext { get; private set; }
        public AiClientRequest? LastRequest { get; private set; }

        public Task<AiExecutionResult> ExecuteAsync(
            AiRequestContext context, AiGovernancePolicy policy, string purpose, AiClientRequest request,
            string? preferredProvider = null, CancellationToken ct = default)
        {
            LastPurpose = purpose;
            LastContext = context;
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private static StubOrchestrator RulesOnly =>
        new(new AiExecutionResult(false, string.Empty, AiOperatingMode.RulesOnly, "Rules", string.Empty, 0, 0, 0, true,
            "Provedor externo não configurado."));

    private static StubOrchestrator External(AiExecutionResult result) => new(result);

    private static MessageDraftAiAssistant Assistant(IAiOrchestrator orchestrator)
    {
        var redaction = new AiRedactionService();
        return new(redaction, new PromptSanitizer(redaction), orchestrator);
    }

    private readonly MessageDraftAiAssistant _assistant = Assistant(RulesOnly);

    // ---------- guardas de governança (fallback/ausência de provedor: template de regras) ----------

    [Fact]
    public async Task Cross_account_request_is_refused_with_empty_draft()
    {
        var result = await _assistant.GenerateDraftAsync(Context(Account), Policy(Guid.NewGuid()), "WhatsApp", "FollowUp", "Maria", "ORC-1", 150, null);
        Assert.Empty(result.DraftContent);
        Assert.False(result.Sent);
        Assert.Contains(result.Disclaimers, x => x.Contains("indispon"));
    }

    [Fact]
    public async Task Inactive_account_is_refused()
    {
        var result = await _assistant.GenerateDraftAsync(Context(Account), Policy(Account, accountActive: false), "WhatsApp", "FollowUp", "Maria", "ORC-1", 150, null);
        Assert.Empty(result.DraftContent);
        Assert.False(result.Sent);
    }

    [Fact]
    public async Task Draft_is_never_sent_and_carries_disclaimer()
    {
        var result = await _assistant.GenerateDraftAsync(Context(Account), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-1", 150, null);
        Assert.False(result.Sent);
        Assert.Contains(result.Disclaimers, x => x.Contains("NUNCA"));
    }

    [Fact]
    public async Task Whatsapp_followup_mentions_client_and_document()
    {
        var result = await _assistant.GenerateDraftAsync(Context(Account), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-2026-01", 150, null);
        Assert.Contains("Maria", result.DraftContent);
        Assert.Contains("ORC-2026-01", result.DraftContent);
        Assert.Equal("WhatsApp", result.Channel);
    }

    [Fact]
    public async Task Email_presentation_is_formal_and_shows_amount()
    {
        var result = await _assistant.GenerateDraftAsync(Context(Account), Policy(Account), "Email", "Presentation", "Maria", "ORC-1", 1500, DateTime.UtcNow.Date.AddDays(15));
        Assert.Contains("Prezado", result.DraftContent);
        Assert.Contains("ORC-1", result.DraftContent);
        Assert.Equal("Email", result.Channel);
    }

    [Fact]
    public async Task Presentation_without_validity_omits_deadline()
    {
        var result = await _assistant.GenerateDraftAsync(Context(Account), Policy(Account), "WhatsApp", "Presentation", "Maria", "ORC-1", 150, null);
        Assert.DoesNotContain("válida até", result.DraftContent);
    }

    [Fact]
    public async Task Customer_name_secrets_are_redacted()
    {
        var result = await _assistant.GenerateDraftAsync(Context(Account), Policy(Account), "WhatsApp", "FollowUp", "Maria token=abcdefghijklmnop", "ORC-1", 150, null);
        Assert.DoesNotContain("abcdefghijklmnop", result.DraftContent);
    }

    // ---------- integração com o orquestrador ----------

    [Fact]
    public async Task Calls_orchestrator_with_message_draft_purpose_and_server_permission()
    {
        var orchestrator = AssistantOrchestrator(out var assistant);
        await assistant.GenerateDraftAsync(Context(Account, withPermission: true), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-9", 150, null);

        Assert.Equal("message_draft", orchestrator.LastPurpose);
        Assert.Contains("Maria", orchestrator.LastRequest?.Prompt);
        Assert.Contains("ORC-9", orchestrator.LastRequest?.Prompt);
        Assert.Contains("Ai.GenerateDrafts", orchestrator.LastContext!.Permissions);
    }

    [Fact]
    public async Task Uses_provider_content_and_adds_ai_disclaimer_on_success()
    {
        var content = "Olá Maria! Passando para lembrar da proposta ORC-9 com valor disponível para conversa quando quiser.";
        var orchestrator = new StubOrchestrator(new AiExecutionResult(true, content, AiOperatingMode.ExternalProvider, "TestProvider", "TestModel", 90, 60, 40, false));
        var assistant = Assistant(orchestrator);

        var result = await assistant.GenerateDraftAsync(Context(Account, withPermission: true), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-9", 150, null);

        Assert.Equal(content, result.DraftContent);
        Assert.False(result.Sent);
        Assert.Contains(result.Disclaimers, x => x.Contains("IA"));
        Assert.Contains(result.Disclaimers, x => x.Contains("NUNCA"));
    }

    [Fact]
    public async Task Provider_result_is_redacted_again_by_the_service()
    {
        var content = "Olá Maria! Lembrando da proposta ORC-9; qualquer dúvida token=abcdefghijklmnop me avise, por favor.";
        var orchestrator = new StubOrchestrator(new AiExecutionResult(true, content, AiOperatingMode.ExternalProvider, "TestProvider", "TestModel", 90, 60, 40, false));
        var assistant = Assistant(orchestrator);

        var result = await assistant.GenerateDraftAsync(Context(Account, withPermission: true), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-9", 150, null);

        Assert.DoesNotContain("abcdefghijklmnop", result.DraftContent);
    }

    [Fact]
    public async Task Short_provider_content_falls_back_to_template()
    {
        var orchestrator = new StubOrchestrator(new AiExecutionResult(true, "ok", AiOperatingMode.ExternalProvider, "TestProvider", "TestModel", 90, 1, 5, false));
        var assistant = Assistant(orchestrator);

        var result = await assistant.GenerateDraftAsync(Context(Account, withPermission: true), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-9", 150, null);

        Assert.Contains("Maria", result.DraftContent);
        Assert.Contains("ORC-9", result.DraftContent);
        Assert.DoesNotContain(result.Disclaimers, x => x.Contains("IA"));
    }

    [Fact]
    public async Task Failed_orchestrator_execution_falls_back_to_template()
    {
        var orchestrator = new StubOrchestrator(new AiExecutionResult(false, string.Empty, AiOperatingMode.RulesOnly, "None", string.Empty, 0, 0, 0, true, "Provedor indisponível."));
        var assistant = Assistant(orchestrator);

        var result = await assistant.GenerateDraftAsync(Context(Account, withPermission: true), Policy(Account), "WhatsApp", "FollowUp", "Maria", "ORC-9", 150, null);

        Assert.Contains("Maria", result.DraftContent);
        Assert.Contains("ORC-9", result.DraftContent);
        Assert.Contains(result.Disclaimers, x => x.Contains("NUNCA"));
        Assert.DoesNotContain(result.Disclaimers, x => x.Contains("IA"));
    }

    [Fact]
    public async Task Feature_disabled_by_plan_skips_orchestrator_entirely()
    {
        var orchestrator = new StubOrchestrator(new AiExecutionResult(true, "Texto de provedor externo.", AiOperatingMode.ExternalProvider, "TestProvider", "TestModel", 90, 60, 40, false));
        var assistant = Assistant(orchestrator);

        var result = await assistant.GenerateDraftAsync(Context(Account, withPermission: true), Policy(Account, featureEnabled: false), "WhatsApp", "FollowUp", "Maria", "ORC-9", 150, null);

        Assert.Null(orchestrator.LastPurpose);
        Assert.Contains("Maria", result.DraftContent);
        Assert.DoesNotContain(result.Disclaimers, x => x.Contains("IA"));
    }

    private static StubOrchestrator AssistantOrchestrator(out MessageDraftAiAssistant assistant)
    {
        var orchestrator = RulesOnly;
        assistant = Assistant(orchestrator);
        return orchestrator;
    }
}
