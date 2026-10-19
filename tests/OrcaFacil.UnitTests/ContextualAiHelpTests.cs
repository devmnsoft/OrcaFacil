using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Web.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// E-3: ajuda contextual com RAG governado. O corpus local (guia da tela + artigos publicados)
/// sempre responde; o provedor externo só entra quando o plano libera, e nada é inventado
/// quando não existe fonte.
/// </summary>
public sealed class ContextualAiHelpTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    private sealed class StubAccount(Guid? accountId, params string[] permissions) : ICurrentAccountService
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid? AccountId { get; } = accountId;
        public Guid? AccountMemberId => null;
        public string? AccountRoleCode => "Owner";
        public AccountStatus? AccountStatus => Domain.Enums.AccountStatus.Active;
        public bool IsPlatformUser => false;
        public bool HasAccount => AccountId.HasValue;
        public Task<bool> HasPermissionAsync(string permissionCode, CancellationToken ct = default) =>
            Task.FromResult(permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase));
        public Task EnsureAccountAccessAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubPlans(bool allowed) : IPlanAccessService
    {
        private PlanAccessDecision Decision(string feature) => allowed
            ? new(true, feature, "FREE", null, 0, null, "Permitido no teste.", "allowed")
            : new(false, feature, "FREE", "PRO", 0, null, "Recurso não incluído no plano atual.", "plan_feature_disabled");

        public Task<OrcaFacil.Domain.Entities.Subscription?> GetCurrentSubscriptionAsync(Guid accountId, CancellationToken ct = default) => Task.FromResult<OrcaFacil.Domain.Entities.Subscription?>(null);
        public Task<OrcaFacil.Domain.Entities.Plan?> GetSelectedPlanAsync(Guid accountId, CancellationToken ct = default) => Task.FromResult<OrcaFacil.Domain.Entities.Plan?>(null);
        public Task<OrcaFacil.Domain.Entities.Plan?> GetEffectivePlanAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<OrcaFacil.Domain.Entities.Plan?>(null);
        public Task<OrcaFacil.Domain.Entities.PlanVersion?> GetEffectivePlanVersionAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<OrcaFacil.Domain.Entities.PlanVersion?>(null);
        public Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetPlanFeaturesAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, PlanFeatureSetting>>(new Dictionary<string, PlanFeatureSetting>());
        public Task<PlanAccessDecision> CanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult(Decision(featureCode));
        public Task<int?> GetLimitAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult<int?>(null);
        public Task<int> GetUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int?> GetRemainingUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult<int?>(null);
        public Task EnsureCanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateAccountCacheAsync(Guid accountId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubKnowledge(params AiSource[] sources) : IHelpKnowledgeSource
    {
        public Task<IReadOnlyList<AiSource>> GetPublishedAsync(Guid accountId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AiSource>>(sources);
    }

    private sealed class StubAiHelp(AiAnswer answer) : IContextualAiHelpService
    {
        public AiRequestContext? LastContext { get; private set; }
        public AiGovernancePolicy? LastPolicy { get; private set; }
        public string? LastQuestion { get; private set; }
        public IReadOnlyList<AiSource> LastSources { get; private set; } = [];

        public Task<AiAnswer> AskHelpAsync(AiRequestContext context, AiGovernancePolicy policy, string question,
            IEnumerable<AiSource> knowledgeBase, CancellationToken ct = default)
        {
            LastContext = context;
            LastPolicy = policy;
            LastQuestion = question;
            LastSources = knowledgeBase.ToList();
            return Task.FromResult(answer);
        }
    }

    private static AiAnswer ExternalAnswer(params AiSource[] sources) => new(
        "Para revogar o link público, abra os detalhes do orçamento e use Revogar link.",
        AiOperatingMode.ExternalProvider, AiConfidence.High, sources, ["Resposta gerada a partir das fontes listadas."], null);

    private static AiAnswer RulesAnswer(params AiSource[] sources) => new(
        "**Detalhes do orçamento**:\nCrie ou revogue o link público na seção de compartilhamento.",
        AiOperatingMode.RulesOnly, AiConfidence.Medium, sources, ["Resposta baseada diretamente no texto das fontes oficiais."], "Revise as fontes documentadas.");

    private static ContextualAiHelpEndpoint Endpoint(
        IContextualAiHelpService aiHelp, ICurrentAccountService account, bool planAllowed = true, params AiSource[] knowledge) =>
        new(new StubKnowledge(knowledge), aiHelp, new ContextualHelpService(), account, new StubPlans(planAllowed));

    [Fact]
    public async Task Sem_conta_selecionada_retorna_403()
    {
        var (status, _) = await Endpoint(new StubAiHelp(ExternalAnswer()), new StubAccount(null)).AskAsync(
            new("Como revogar o link?", "document.details"), CancellationToken.None);
        Assert.Equal(403, status);
    }

    [Fact]
    public async Task Pergunta_curta_retorna_400()
    {
        var (status, _) = await Endpoint(new StubAiHelp(ExternalAnswer()), new StubAccount(AccountId)).AskAsync(
            new("oi", "dashboard"), CancellationToken.None);
        Assert.Equal(400, status);
    }

    [Fact]
    public async Task Guia_da_tela_alimenta_o_corpus_mesmo_sem_artigos()
    {
        var stub = new StubAiHelp(RulesAnswer());
        var (status, body) = await Endpoint(stub, new StubAccount(AccountId, "Ai.View")).AskAsync(
            new("Como revogar o link público?", "document.details"), CancellationToken.None);

        Assert.Equal(200, status);
        Assert.Contains(stub.LastSources, s => s.Type == "guide" && s.Title == "Detalhes do orçamento");
        Assert.Equal("Detalhes do orçamento", stub.LastSources[0].Title);
        Assert.NotNull(body);
    }

    [Fact]
    public async Task Artigos_publicados_entram_no_corpus_com_url_segura()
    {
        var article = new AiSource(AccountId, "article", Guid.NewGuid().ToString(), "Como revogar links",
            "/Help/Article?slug=como-revogar-links", "conteúdo publicado");
        var stub = new StubAiHelp(RulesAnswer(article));
        await Endpoint(stub, new StubAccount(AccountId, "Ai.View"), knowledge: article).AskAsync(
            new("Como revogar o link?", "documents"), CancellationToken.None);

        Assert.Contains(stub.LastSources, s => s.Title == "Como revogar links" && s.Url.StartsWith("/Help/Article"));
    }

    [Fact]
    public async Task Permissoes_de_IA_sao_repassadas_ao_contexto()
    {
        var stub = new StubAiHelp(ExternalAnswer());
        await Endpoint(stub, new StubAccount(AccountId, "Ai.View", "Ai.GenerateDrafts")).AskAsync(
            new("Como revogar o link?", "document.details"), CancellationToken.None);

        Assert.NotNull(stub.LastContext);
        Assert.Contains("Ai.View", stub.LastContext!.Permissions);
        Assert.Contains("Ai.GenerateDrafts", stub.LastContext.Permissions);
        Assert.DoesNotContain("Ai.ApplySuggestions", stub.LastContext.Permissions);
    }

    [Fact]
    public async Task Resposta_do_provedor_marca_apoio_de_IA_e_preserva_fontes()
    {
        var source = new AiSource(AccountId, "guide", "document.details", "Detalhes do orçamento", "/Documents/Details", "texto");
        var stub = new StubAiHelp(ExternalAnswer(source));
        var (_, body) = await Endpoint(stub, new StubAccount(AccountId, "Ai.View")).AskAsync(
            new("Como revogar o link?", "document.details"), CancellationToken.None);

        var response = Assert.IsType<ContextualAiHelpEndpoint.AskHelpResponse>(body);
        Assert.True(response.Succeeded);
        Assert.True(response.IsProviderGenerated);
        Assert.Contains("apoio de IA", response.Notice);
        Assert.Single(response.Sources);
        Assert.Equal("/Documents/Details", response.Sources[0].Url);
        Assert.NotNull(stub.LastPolicy);
        Assert.True(stub.LastPolicy!.FeatureEnabled && stub.LastPolicy.AccountActive);
    }

    [Fact]
    public async Task Sem_provedor_o_aviso_diz_que_nada_foi_executado()
    {
        var source = new AiSource(AccountId, "guide", "documents", "Orçamentos", "/Documents/Index", "texto");
        var stub = new StubAiHelp(RulesAnswer(source));
        var (_, body) = await Endpoint(stub, new StubAccount(AccountId, "Ai.View"), planAllowed: false).AskAsync(
            new("Como acompanhar propostas?", "documents"), CancellationToken.None);

        var response = Assert.IsType<ContextualAiHelpEndpoint.AskHelpResponse>(body);
        Assert.False(response.IsProviderGenerated);
        Assert.Contains("Nenhuma ação foi executada", response.Notice);
        Assert.NotNull(stub.LastPolicy);
        Assert.False(stub.LastPolicy!.FeatureEnabled);

        var providerResponse = await Endpoint(stub, new StubAccount(AccountId, "Ai.View"), planAllowed: true).AskAsync(
            new("Como acompanhar propostas?", "documents"), CancellationToken.None);
        Assert.True(stub.LastPolicy!.FeatureEnabled);
    }

    [Fact]
    public async Task Sem_fonte_relevante_a_resposta_admite_falta_de_evidencia()
    {
        var insufficient = new AiAnswer(
            "A base autorizada não possui informação suficiente para responder a esta dúvida.",
            AiOperatingMode.SecureRag, AiConfidence.Insufficient, [],
            ["Nenhuma fonte acessível e documentada foi encontrada para o termo pesquisado."]);
        var stub = new StubAiHelp(insufficient);
        var (status, body) = await Endpoint(stub, new StubAccount(AccountId, "Ai.View")).AskAsync(
            new("Como revogar o link?", "dashboard"), CancellationToken.None);

        var response = Assert.IsType<ContextualAiHelpEndpoint.AskHelpResponse>(body);
        Assert.Equal(200, status);
        Assert.False(response.Succeeded);
        Assert.Equal("Insufficient", response.Confidence);
        Assert.Empty(response.Sources);
        Assert.Contains("Nenhuma ação foi executada", response.Notice);
        Assert.NotNull(stub.LastContext);
    }

    [Fact]
    public async Task Codigo_de_ajuda_desconhecido_nao_quebra()
    {
        var stub = new StubAiHelp(RulesAnswer());
        var (status, _) = await Endpoint(stub, new StubAccount(AccountId, "Ai.View")).AskAsync(
            new("Como revogar o link?", "codigo-inexistente"), CancellationToken.None);
        Assert.Equal(200, status);
    }
}
