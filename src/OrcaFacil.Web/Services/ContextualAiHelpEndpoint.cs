using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Web.Services;

/// <summary>
/// E-3: liga a ajuda contextual existente (guias estáticos da tela + artigos publicados da
/// base de conhecimento) ao RAG governado. A resposta é sempre fundamentada em fontes
/// autorizadas; sem fonte relevante o serviço diz que não há evidência — nunca inventa.
/// </summary>
public sealed class ContextualAiHelpEndpoint(
    IHelpKnowledgeSource knowledge,
    IContextualAiHelpService aiHelp,
    IContextualHelpService help,
    ICurrentAccountService account,
    IPlanAccessService plans)
{
    public sealed record AskHelpRequest(string? Question, string? HelpCode);

    public sealed record AskHelpResponse(
        bool Succeeded,
        string Answer,
        string Mode,
        string Confidence,
        IReadOnlyList<HelpSourceDto> Sources,
        IReadOnlyList<string> Limitations,
        string? SuggestedNextAction,
        bool IsProviderGenerated,
        string Notice);

    public sealed record HelpSourceDto(string Title, string Url, string Type);

    public async Task<(int Status, object? Body)> AskAsync(AskHelpRequest input, CancellationToken ct)
    {
        var accountId = account.AccountId;
        if (!accountId.HasValue) return (StatusCodes.Status403Forbidden, new { message = "Selecione uma conta para usar a ajuda inteligente." });

        try { await account.EnsureAccountAccessAsync(ct); }
        catch (UnauthorizedAccessException) { return (StatusCodes.Status403Forbidden, new { message = "Sem acesso a esta conta." }); }

        var question = (input.Question ?? string.Empty).Trim();
        if (question.Length < 3)
            return (StatusCodes.Status400BadRequest, new { message = "Descreva sua dúvida com pelo menos 3 caracteres." });
        if (question.Length > 500) question = question[..500];

        var allowed = await plans.CanUseAsync(accountId.Value, PlanFeatureCodes.AiExternalProvidersEnabled, ct);

        // A ajuda nunca depende de IA externa: os guias da tela e os artigos publicados formam
        // o corpus local. O benefício do plano só libera o provedor externo (quando configurado).
        var sources = await BuildSourcesAsync(accountId.Value, input.HelpCode, ct);
        if (sources.Count == 0)
            return (StatusCodes.Status200OK, new AskHelpResponse(false, string.Empty, "RulesOnly", "Insufficient", [],
                ["A base autorizada não possui artigos publicados para esta conta."],
                "Abra a Central de Ajuda para ver os guias disponíveis.", false,
                "Nenhuma fonte documentada foi encontrada; nada foi inventado."));

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (await account.HasPermissionAsync("Ai.View", ct)) permissions.Add("Ai.View");
        if (await account.HasPermissionAsync("Ai.GenerateDrafts", ct)) permissions.Add("Ai.GenerateDrafts");
        if (await account.HasPermissionAsync("Ai.ApplySuggestions", ct)) permissions.Add("Ai.ApplySuggestions");

        var context = new AiRequestContext(accountId.Value, account.UserId, permissions);
        var policy = new AiGovernancePolicy(accountId.Value,
            AccountActive: account.AccountStatus is null or Domain.Enums.AccountStatus.Active,
            FeatureEnabled: allowed.IsAllowed);

        var answer = await aiHelp.AskHelpAsync(context, policy, question, sources, ct);

        var isProvider = answer.Mode == AiOperatingMode.ExternalProvider;
        var notice = isProvider
            ? "Resposta sugerida com apoio de IA a partir das fontes listadas. Confira antes de agir — nada foi alterado na sua conta."
            : "Resposta montada a partir do texto das fontes oficiais listadas. Nenhuma ação foi executada.";

        // "Succeeded" é sobre ter resposta fundamentada, não sobre ter executado algo.
        var succeeded = answer.Confidence != AiConfidence.Insufficient;

        return (StatusCodes.Status200OK, new AskHelpResponse(
            succeeded,
            answer.Text,
            answer.Mode.ToString(),
            answer.Confidence.ToString(),
            answer.Sources.Select(x => new HelpSourceDto(x.Title, x.Url, x.Type)).ToArray(),
            answer.Limitations,
            answer.SuggestedNextAction,
            isProvider,
            notice));
    }

    private async Task<IReadOnlyList<AiSource>> BuildSourcesAsync(Guid accountId, string? helpCode, CancellationToken ct)
    {
        var sources = new List<AiSource>();

        // 1) Guia da tela atual primeiro (foco da pergunta), depois todo o corpus de guias do
        // produto: o conteúdo é determinístico e sempre autorizado, sem depender de IA externa.
        var current = string.IsNullOrWhiteSpace(helpCode) ? null : help.Find(helpCode);
        if (current is not null) sources.Add(ToSource(accountId, current));

        foreach (var guide in help.All())
        {
            if (current is not null && guide.Code.Equals(current.Code, StringComparison.OrdinalIgnoreCase)) continue;
            sources.Add(ToSource(accountId, guide));
        }

        // 2) Artigos publicados da base de conhecimento (global do produto, somente publicados).
        sources.AddRange(await knowledge.GetPublishedAsync(accountId, ct));

        return sources;
    }

    private static AiSource ToSource(Guid accountId, HelpContentDefinition guide) =>
        new(accountId, "guide", guide.Code, guide.Title, guide.RelatedPage,
            $"{guide.Summary}\n{guide.WhenToUse}\n{guide.BeforeStarting}\n{string.Join(" ", guide.Steps)}\n{guide.WhatHappensNext}");
}
