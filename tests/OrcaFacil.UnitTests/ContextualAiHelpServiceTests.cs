using OrcaFacil.Application.Ai;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// E-3: contrato do RAG de ajuda contextual. A resposta é sempre fundamentada; o provedor
/// externo só é chamado com o plano liberado (a política chega ao orquestrador), e o texto
/// do provedor é redigido antes de voltar para a tela.
/// </summary>
public sealed class ContextualAiHelpServiceTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    private sealed class RecordingOrchestrator(AiExecutionResult result) : IAiOrchestrator
    {
        public int Calls { get; private set; }
        public AiGovernancePolicy? LastPolicy { get; private set; }
        public string? LastPurpose { get; private set; }
        public AiClientRequest? LastRequest { get; private set; }

        public Task<AiExecutionResult> ExecuteAsync(
            AiRequestContext context,
            AiGovernancePolicy policy,
            string purpose,
            AiClientRequest request,
            string? preferredProvider = null,
            CancellationToken ct = default)
        {
            Calls++;
            LastPolicy = policy;
            LastPurpose = purpose;
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private static AiExecutionResult ProviderResult(string content) => new(
        true, content, AiOperatingMode.ExternalProvider, "Provider", "model", 10, 10, 5, false);

    private static AiExecutionResult FallbackResult() => new(
        false, string.Empty, AiOperatingMode.RulesOnly, "Rules", "Rules", 0, 0, 0, true,
        "Nenhum provedor externo está habilitado ou com credenciais configuradas.");

    private static ContextualAiHelpService Service(IAiOrchestrator orchestrator)
    {
        var redaction = new AiRedactionService();
        return new ContextualAiHelpService(new AiPromptInjectionGuard(), orchestrator, new PromptSanitizer(redaction), redaction);
    }

    private static AiSource Source(string title, string content, Guid? accountId = null, bool accessible = true) =>
        new(accountId ?? AccountId, "guide", title.ToLowerInvariant(), title, "/Documents", content, accessible);

    private static AiRequestContext Context(params string[] permissions) =>
        new(AccountId, Guid.NewGuid(), new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase));

    [Fact]
    public async Task Sem_fonte_relevante_nao_chama_provedor()
    {
        var orchestrator = new RecordingOrchestrator(ProviderResult("não deveria ser usado"));
        var answer = await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(AccountId), "Como emitir recibo?",
            [Source("Orçamentos", "Acompanhe propostas enviadas e validade.")]);

        Assert.Equal(0, orchestrator.Calls);
        Assert.Equal(AiConfidence.Insufficient, answer.Confidence);
        Assert.Empty(answer.Sources);
    }

    [Fact]
    public async Task Fonte_de_outra_conta_e_inacessivel_nunca_entram()
    {
        var orchestrator = new RecordingOrchestrator(ProviderResult("resposta"));
        var answer = await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(AccountId), "Como faturar o orçamento?",
            [
                Source("Faturamento interno", "Regra de faturamento do orçamento.", Guid.NewGuid()),
                Source("Faturamento oculto", "Regra de faturamento do orçamento.", accessible: false)
            ]);

        Assert.Equal(0, orchestrator.Calls);
        Assert.Equal(AiConfidence.Insufficient, answer.Confidence);
    }

    [Fact]
    public async Task Acentuacao_da_pergunta_casa_com_a_fonte()
    {
        var orchestrator = new RecordingOrchestrator(FallbackResult());
        var answer = await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(AccountId), "como funciona o orcamento?",
            [Source("Orçamento", "O orçamento reúne itens, validade e condições.")]);

        Assert.Single(answer.Sources);
        Assert.Equal("Orçamento", answer.Sources[0].Title);
    }

    [Fact]
    public async Task Palavras_genericas_sozinhas_nao_geram_resposta_fundamentada()
    {
        var orchestrator = new RecordingOrchestrator(ProviderResult("resposta"));
        var answer = await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(AccountId), "como faço isso aqui?",
            [Source("Qualquer guia", "Texto que contém como e aqui em toda frase.")]);

        Assert.Equal(0, orchestrator.Calls);
        Assert.Equal(AiConfidence.Insufficient, answer.Confidence);
    }

    [Fact]
    public async Task Politica_do_plano_chega_ao_orquestrador()
    {
        var orchestrator = new RecordingOrchestrator(FallbackResult());
        var policy = new AiGovernancePolicy(AccountId, AccountActive: true, FeatureEnabled: false);

        await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), policy, "Como revogar o link público?",
            [Source("Link público", "Revogue o link público nos detalhes do orçamento.")]);

        Assert.Equal(1, orchestrator.Calls);
        Assert.NotNull(orchestrator.LastPolicy);
        Assert.False(orchestrator.LastPolicy!.FeatureEnabled);
        Assert.Equal("contextual_help", orchestrator.LastPurpose);
    }

    [Fact]
    public async Task Conta_do_contexto_diferente_da_politica_bloqueia_a_consulta()
    {
        var orchestrator = new RecordingOrchestrator(ProviderResult("resposta"));
        var answer = await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(Guid.NewGuid()), "Como revogar o link público?",
            [Source("Link público", "Revogue o link público nos detalhes do orçamento.")]);

        Assert.Equal(0, orchestrator.Calls);
        Assert.Equal(AiConfidence.Insufficient, answer.Confidence);
    }

    [Fact]
    public async Task Resposta_do_provedor_e_redigida_e_marcada_como_externa()
    {
        var orchestrator = new RecordingOrchestrator(
            ProviderResult("Use o token sk_live_1234567890ABCDEF para concluir."));
        var answer = await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(AccountId), "Como revogar o link público?",
            [Source("Link público", "Revogue o link público nos detalhes do orçamento.")]);

        Assert.Equal(AiOperatingMode.ExternalProvider, answer.Mode);
        Assert.DoesNotContain("sk_live_1234567890ABCDEF", answer.Text);
        Assert.Contains("[DADO SENSÍVEL REMOVIDO]", answer.Text);
        Assert.Single(answer.Sources);
    }

    [Fact]
    public async Task Segredo_na_pergunta_nao_vaza_para_o_provedor()
    {
        var orchestrator = new RecordingOrchestrator(FallbackResult());
        await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(AccountId),
            "Minha senha: SegredoForte123, como revogar o link público?",
            [Source("Link público", "Revogue o link público nos detalhes do orçamento.")]);

        Assert.NotNull(orchestrator.LastRequest);
        Assert.DoesNotContain("SegredoForte123", orchestrator.LastRequest!.Prompt);
    }

    [Fact]
    public async Task Fallback_do_provedor_devolve_texto_das_fontes()
    {
        var orchestrator = new RecordingOrchestrator(FallbackResult());
        var answer = await Service(orchestrator).AskHelpAsync(
            Context("Ai.View"), new AiGovernancePolicy(AccountId), "Como revogar o link público?",
            [Source("Link público", "Revogue o link público nos detalhes do orçamento.")]);

        Assert.Equal(AiOperatingMode.RulesOnly, answer.Mode);
        Assert.Contains("**Link público**", answer.Text);
        Assert.Contains("Revogue o link público", answer.Text);
        Assert.Single(answer.Sources);
    }
}
