using System.Text.Json;
using OrcaFacil.Application.Ai;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>E-2: o revisor comercial consulta o orquestrador e mantém o motor de regras como rede de segurança.</summary>
public sealed class CommercialAiReviewerAiTests
{
    private static readonly Guid Account = Guid.NewGuid();

    private static AiRequestContext Context() =>
        new(Account, Guid.NewGuid(), new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ai.ApplySuggestions", "documents.edit" });

    private static AiGovernancePolicy Policy(bool featureEnabled = true) =>
        new(Account, AccountActive: true, FeatureEnabled: featureEnabled);

    private static Document Doc(bool withValidity = false) => new()
    {
        ClientName = "Maria Souza",
        Status = "Draft",
        ValidUntil = withValidity ? DateTime.UtcNow.Date.AddDays(10) : null,
        ConditionsText = null,
    };

    private static IReadOnlyList<DocumentItem> Items(int count = 1, decimal price = 100) =>
        Enumerable.Range(0, count).Select(_ => new DocumentItem { Description = "Instalação de luminária", Quantity = 1, UnitPrice = price, Unit = "serviço" }).ToList();

    private static Client Client() => new() { Name = "Maria Souza", Email = "maria@exemplo.com", Phone = "11999990000" };

    private sealed class StubOrchestrator(AiExecutionResult result) : IAiOrchestrator
    {
        public string? LastPurpose { get; private set; }
        public AiClientRequest? LastRequest { get; private set; }

        public Task<AiExecutionResult> ExecuteAsync(
            AiRequestContext context, AiGovernancePolicy policy, string purpose, AiClientRequest request,
            string? preferredProvider = null, CancellationToken ct = default)
        {
            LastPurpose = purpose;
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private static StubOrchestrator Fallback => new(
        new AiExecutionResult(false, string.Empty, AiOperatingMode.RulesOnly, "Rules", string.Empty, 0, 0, 0, true, "Nenhum provedor externo está habilitado."));

    private static StubOrchestrator Produce(string content) => new(
        new AiExecutionResult(true, content, AiOperatingMode.ExternalProvider, "TestProvider", "TestModel", 120, 90, 35, false));

    private static string ProviderJson(bool valid = true, bool withSecret = false)
    {
        if (!valid) return "Desculpe, não sei o que dizer em JSON.";
        return JsonSerializer.Serialize(new
        {
            title = "Proposta de Instalação - Maria Souza" + (withSecret ? " senha=segredo123" : string.Empty),
            conditions = "Prazo de execução a confirmar após visita técnica.",
            findings = new[]
            {
                new { category = "Itens", severity = "Critica", message = "Falta descrição detalhada do serviço de instalação.", suggestion = "Descreva o escopo técnico." },
                new { category = "Geral", severity = "URGENTE!!", message = "Solicite a assinatura digital antes da execução.", suggestion = string.Empty },
                new { category = "Geral", severity = "Media", message = "Instalação de luminária", suggestion = string.Empty },
                new { category = "Geral", severity = "Media", message = "apontamento extra", suggestion = string.Empty },
            },
        });
    }

    [Fact]
    public async Task Sem_orquestrador_mantém_resultado_de_regras()
    {
        var reviewer = new CommercialAiReviewer();
        var contatoVazio = new Client { Name = "Maria Souza", Email = null, Phone = null };
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(), Items(), contatoVazio);
        Assert.True(result.Succeeded);
        Assert.True(result.IsRuleBased);
        Assert.Contains(result.Findings, f => f.Category == "Condições");
        Assert.Contains(result.Findings, f => f.Category == "Cliente" && f.Severity == "Média");
    }

    [Fact]
    public async Task Provedor_indisponível_é_idêntico_às_regras()
    {
        var reviewer = new CommercialAiReviewer(Fallback, new PromptSanitizer(new AiRedactionService()), new AiRedactionService());
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(), Items(), Client());
        Assert.True(result.IsRuleBased);
        Assert.Contains(result.Findings, f => f.Category == "Condições");
    }

    [Fact]
    public async Task Provedor_responde_com_json_válido_e_mantém_regras()
    {
        var orchestrator = Produce(ProviderJson());
        var reviewer = new CommercialAiReviewer(orchestrator, new PromptSanitizer(new AiRedactionService()), new AiRedactionService());
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(), Items(), Client());

        Assert.False(result.IsRuleBased);
        Assert.Equal("quote_review", orchestrator.LastPurpose);
        Assert.Equal("Proposta de Instalação - Maria Souza", result.SuggestedTitle);
        Assert.Contains("visita técnica", result.SuggestedConditions);
        // achado crítico do proven. + regras determinísticas (validade em falta)
        Assert.Contains(result.Findings, f => f.Severity == "Crítica" && f.Category == "Itens");
        Assert.Contains(result.Findings, f => f.Category == "Condições");
    }

    [Fact]
    public async Task Severidade_invalida_é_normalizada()
    {
        var reviewer = new CommercialAiReviewer(Produce(ProviderJson()), new PromptSanitizer(new AiRedactionService()), new AiRedactionService());
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(), Items(), Client());
        var unknown = result.Findings.First(f => f.Message.Contains("assinatura digital"));
        Assert.Equal("Média", unknown.Severity);
    }

    [Fact]
    public async Task Achados_duplicados_não_duplicam()
    {
        var reviewer = new CommercialAiReviewer(Produce(ProviderJson()), new PromptSanitizer(new AiRedactionService()), new AiRedactionService());
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(), Items(), Client());
        Assert.Equal(1, result.Findings.Count(f => f.Message.Equals("Instalação de luminária", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Json_invalido_cai_nas_regras()
    {
        var reviewer = new CommercialAiReviewer(Produce(ProviderJson(valid: false)), new PromptSanitizer(new AiRedactionService()), new AiRedactionService());
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(withValidity: true), Items(), Client());
        Assert.True(result.IsRuleBased);
        Assert.Equal("Proposta Comercial - Maria Souza", result.SuggestedTitle);
        Assert.Empty(result.Findings); // conta ativa, cliente com contato, item válido, validade futura
    }

    [Fact]
    public async Task Segredos_do_provedor_são_redigidos()
    {
        var reviewer = new CommercialAiReviewer(Produce(ProviderJson(withSecret: true)), new PromptSanitizer(new AiRedactionService()), new AiRedactionService());
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(), Items(), Client());
        Assert.DoesNotContain("segredo123", result.SuggestedTitle);
    }

    [Fact]
    public async Task Findings_do_provedor_são_capped_em_oito()
    {
        var findingsJson = string.Join(",", Enumerable.Range(0, 14).Select(i =>
            JsonSerializer.Serialize(new { category = "Item", severity = "Baixa", message = "apontamento " + i })));
        var json = JsonSerializer.Serialize(new { title = "T", conditions = "C", findings = JsonSerializer.Deserialize<JsonElement>("[" + findingsJson + "]") });
        var reviewer = new CommercialAiReviewer(Produce(json), new PromptSanitizer(new AiRedactionService()), new AiRedactionService());
        var result = await reviewer.ReviewQuoteAsync(Context(), Policy(), Doc(), Items(), Client());
        Assert.True(result.Findings.Count(f => f.Message.StartsWith("apontamento ")) <= 8);
    }

    [Fact]
    public void Parser_aceita_json_em_cerca_de_código()
    {
        var parsed = CommercialReviewParser.Parse("```json\n{\"title\":\"T\",\"conditions\":\"C\",\"findings\":[{\"category\":\"Cliente\",\"severity\":\"alta\",\"message\":\"contato ausente\"}]}\n```");
        Assert.Equal("T", parsed.SuggestedTitle);
        Assert.Contains(parsed.Findings, f => f.Severity == "Alta");
    }

    [Fact]
    public void Parser_rejeita_sem_json()
    {
        var parsed = CommercialReviewParser.Parse("sem json aqui");
        Assert.Empty(parsed.Findings);
        Assert.Equal(string.Empty, parsed.SuggestedTitle);
    }
}
