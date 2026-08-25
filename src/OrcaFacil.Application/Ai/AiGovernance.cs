using System.Text.RegularExpressions;

namespace OrcaFacil.Application.Ai;

public enum AiOperatingMode { RulesOnly, ExternalProvider, SecureRag }
public enum AiProviderStatus { NotConfigured, Configured, Healthy, Degraded, Failed, Disabled }
public enum AiConfidence { Insufficient, Low, Medium, High }

public sealed record AiRequestContext(Guid AccountId, Guid UserId, IReadOnlySet<string> Permissions);
public sealed record AiSource(Guid AccountId, string Type, string EntityId, string Title, string Url, string Content, bool IsAccessible = true);
public sealed record AiAnswer(string Text, AiOperatingMode Mode, AiConfidence Confidence,
    IReadOnlyList<AiSource> Sources, IReadOnlyList<string> Limitations, string? SuggestedNextAction = null);
public sealed record AiProviderConfiguration(bool Enabled, string? Provider, string? ChatModel,
    string? EmbeddingModel, string? ProtectedCredential, TimeSpan Timeout)
{
    public AiProviderStatus Status => !Enabled ? AiProviderStatus.Disabled
        : string.IsNullOrWhiteSpace(Provider) || string.IsNullOrWhiteSpace(ChatModel) || string.IsNullOrWhiteSpace(ProtectedCredential)
            ? AiProviderStatus.NotConfigured : AiProviderStatus.Configured;
}

public interface IAiChatClient { Task<string> CompleteAsync(string sanitizedPrompt, CancellationToken cancellationToken); }
public interface IAiEmbeddingClient { Task<IReadOnlyList<float>> EmbedAsync(string sanitizedText, CancellationToken cancellationToken); }
public interface IAiDocumentAnalysisClient { Task<string> AnalyzeAsync(string sanitizedText, CancellationToken cancellationToken); }
public interface IAiProvider
{
    string Name { get; }
    AiOperatingMode Mode { get; }
    AiProviderStatus Status { get; }
    IAiChatClient? Chat { get; }
    IAiEmbeddingClient? Embeddings { get; }
    IAiDocumentAnalysisClient? Documents { get; }
}

public sealed class NoopAiProvider : IAiProvider
{
    public string Name => "Nenhum provedor";
    public AiOperatingMode Mode => AiOperatingMode.RulesOnly;
    public AiProviderStatus Status => AiProviderStatus.NotConfigured;
    public IAiChatClient? Chat => null;
    public IAiEmbeddingClient? Embeddings => null;
    public IAiDocumentAnalysisClient? Documents => null;
}

public sealed class RulesOnlyAiProvider : IAiProvider
{
    public string Name => "Regras internas";
    public AiOperatingMode Mode => AiOperatingMode.RulesOnly;
    public AiProviderStatus Status => AiProviderStatus.Healthy;
    public IAiChatClient? Chat => null;
    public IAiEmbeddingClient? Embeddings => null;
    public IAiDocumentAnalysisClient? Documents => null;
}

public sealed class ExternalAiProvider : IAiProvider
{
    public ExternalAiProvider(AiProviderConfiguration configuration, IAiChatClient chat,
        IAiEmbeddingClient? embeddings = null, IAiDocumentAnalysisClient? documents = null)
    {
        if (configuration.Status != AiProviderStatus.Configured)
            throw new InvalidOperationException("O provedor externo não está configurado.");
        Name = configuration.Provider!; Chat = chat; Embeddings = embeddings; Documents = documents;
    }
    public string Name { get; }
    public AiOperatingMode Mode => AiOperatingMode.ExternalProvider;
    public AiProviderStatus Status => AiProviderStatus.Configured;
    public IAiChatClient? Chat { get; }
    public IAiEmbeddingClient? Embeddings { get; }
    public IAiDocumentAnalysisClient? Documents { get; }
}

public sealed record AiGovernancePolicy(Guid AccountId, bool AllowCustomerData = true,
    bool AllowFinancialData = false, bool AllowCostAndMargin = false, bool AllowDocuments = false,
    bool AllowCommercialDrafts = true, bool AllowFinancialDrafts = false, bool AllowSuggestions = true,
    bool AllowAutomaticCriticalActions = false);

public sealed class AiGovernanceService
{
    public bool CanUse(AiRequestContext context, AiGovernancePolicy policy, string permission) =>
        context.AccountId != Guid.Empty && context.AccountId == policy.AccountId && context.Permissions.Contains(permission);

    public bool CanUseFinancialData(AiRequestContext context, AiGovernancePolicy policy) =>
        CanUse(context, policy, "Ai.View") && policy.AllowFinancialData && context.Permissions.Contains("Finance.View");

    public bool CanUseCostAndMargin(AiRequestContext context, AiGovernancePolicy policy) =>
        CanUseFinancialData(context, policy) && policy.AllowCostAndMargin && context.Permissions.Contains("Margins.View");

    public bool CanExecuteAutomatically(string action) => false;
}

public interface IAiRedactionService { string Sanitize(string? value); }
public sealed partial class AiRedactionService : IAiRedactionService
{
    private const string Mask = "[DADO SENSÍVEL REMOVIDO]";
    [GeneratedRegex(@"(?im)(password|senha|api[_ -]?key|token|webhook[_ -]?secret|smtp[_ -]?(?:password|secret)|connection[_ -]?string|storagepath|gateway[_ -]?secret)\s*[:=]\s*([^\s,;]+)")]
    private static partial Regex NamedSecret();
    [GeneratedRegex(@"(?i)\b(?:sk|pk)_(?:live|test)_[a-z0-9_-]{12,}\b|\bBearer\s+[a-z0-9._~+/-]{12,}=*\b")]
    private static partial Regex Token();
    [GeneratedRegex(@"\b\d{4}[ .-]?\d{4}[ .-]?\d{4}[ .-]?\d{4}\b")]
    private static partial Regex Card();

    public string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sanitized = NamedSecret().Replace(value, m => $"{m.Groups[1].Value}={Mask}");
        sanitized = Token().Replace(sanitized, Mask);
        return Card().Replace(sanitized, "**** **** **** ****");
    }
}

public sealed class PromptSanitizer(IAiRedactionService redaction)
{
    public string Sanitize(string prompt) => redaction.Sanitize(prompt).Trim();
}

public sealed partial class AiPromptInjectionGuard
{
    [GeneratedRegex(@"(?i)(ignore|disregard|bypass|override).{0,35}(instruction|permission|policy|system)|system\s*prompt|revele?.{0,20}(token|senha|segredo)")]
    private static partial Regex SuspiciousInstruction();
    public bool IsSuspicious(string? content) => !string.IsNullOrWhiteSpace(content) && SuspiciousInstruction().IsMatch(content);
    public string AsUntrustedSource(string content) => $"<fonte_nao_confiavel>\n{content}\n</fonte_nao_confiavel>";
}

public static class AiActionPolicy
{
    public static readonly IReadOnlySet<string> AllowedDrafts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "task", "alert", "internal_note", "message_draft", "checklist_draft", "response_draft", "status_suggestion" };
    public static readonly IReadOnlySet<string> Prohibited = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "payment", "receipt", "fiscal", "permission", "delete", "anonymize", "contract_cancel", "account_suspend", "proposal_approve", "send_email", "send_whatsapp" };
    public static bool CanCreateDraft(string action) => AllowedDrafts.Contains(action) && !Prohibited.Contains(action);
}

public sealed record AiActionDraft(Guid Id, Guid AccountId, Guid RequestedBy, string Action, string Payload, bool Approved = false);
public sealed class AiActionDraftService(IAiRedactionService redaction)
{
    public AiActionDraft Create(AiRequestContext context, string action, string payload)
    {
        if (!context.Permissions.Contains("Ai.ApplySuggestions")) throw new UnauthorizedAccessException();
        if (!AiActionPolicy.CanCreateDraft(action)) throw new InvalidOperationException("Esta ação não pode ser criada ou executada por recursos inteligentes.");
        return new(Guid.NewGuid(), context.AccountId, context.UserId, action, redaction.Sanitize(payload));
    }
}

public sealed record AiQuota(int MonthlyAccountLimit, int DailyUserLimit, int MonthlyUsed, int DailyUsed);
public sealed class AiQuotaService
{
    public const string LimitMessage = "Você atingiu o limite de recursos inteligentes do seu plano.";
    public void EnsureAvailable(AiQuota quota)
    {
        if (quota.MonthlyUsed >= quota.MonthlyAccountLimit || quota.DailyUsed >= quota.DailyUserLimit)
            throw new InvalidOperationException(LimitMessage);
    }
}

public sealed class AiRagService(AiPromptInjectionGuard injectionGuard)
{
    public AiAnswer BuildGroundedAnswer(AiRequestContext context, string answer, IEnumerable<AiSource> candidates)
    {
        var sources = candidates.Where(x => x.AccountId == context.AccountId && x.IsAccessible && !injectionGuard.IsSuspicious(x.Content)).ToArray();
        if (sources.Length == 0) return new("A base autorizada não possui informação suficiente para responder.", AiOperatingMode.SecureRag,
            AiConfidence.Insufficient, [], ["Nenhuma fonte acessível e suficiente foi encontrada."]);
        return new(answer, AiOperatingMode.SecureRag, sources.Length > 1 ? AiConfidence.High : AiConfidence.Medium,
            sources, [], "Revise as fontes antes de tomar uma decisão.");
    }
}

public sealed record SemanticSearchResult(Guid AccountId, string Type, string Title, string Url, string Source, decimal Score);
public sealed record SemanticSearchResponse(bool AdvancedSemanticSearchConfigured, IReadOnlyList<SemanticSearchResult> Results, string Status);
public sealed class SemanticSearchService
{
    public SemanticSearchResponse Filter(AiRequestContext context, IEnumerable<SemanticSearchResult> candidates, bool embeddingsConfigured)
    {
        if (!context.Permissions.Contains("Ai.UseSemanticSearch")) throw new UnauthorizedAccessException();
        var results = candidates.Where(x => x.AccountId == context.AccountId && Uri.IsWellFormedUriString(x.Url, UriKind.RelativeOrAbsolute))
            .OrderByDescending(x => x.Score).ToArray();
        return new(embeddingsConfigured, results, embeddingsConfigured ? "Busca semântica avançada" : "Busca semântica avançada não configurada; usando busca textual normalizada.");
    }
}

public sealed record CopilotFact(Guid AccountId, string Category, string Text, string SourceUrl, bool ContainsFinancialData = false);
public sealed class CopilotService(AiGovernanceService governance)
{
    public AiAnswer Answer(AiRequestContext context, AiGovernancePolicy policy, IEnumerable<CopilotFact> facts)
    {
        if (!governance.CanUse(context, policy, "Ai.UseCopilot")) throw new UnauthorizedAccessException();
        var allowed = facts.Where(x => x.AccountId == context.AccountId && (!x.ContainsFinancialData || governance.CanUseFinancialData(context, policy))).ToArray();
        if (allowed.Length == 0) return new("Não encontrei dados permitidos suficientes para responder.", AiOperatingMode.RulesOnly,
            AiConfidence.Insufficient, [], ["A resposta respeita as permissões e a conta selecionada."]);
        var sources = allowed.Select((x, index) => new AiSource(context.AccountId, x.Category, index.ToString(), x.Category, x.SourceUrl, x.Text)).ToArray();
        return new(string.Join(Environment.NewLine, allowed.Select(x => x.Text)), AiOperatingMode.RulesOnly,
            AiConfidence.High, sources, ["Resultado calculado por regras internas; nenhum provedor externo foi chamado."]);
    }
}

public sealed record AiDocumentAnalysis(bool Analyzed, string Summary, IReadOnlyList<string> Limitations, AiSource? Source);
public sealed class AiDocumentAnalysisService(AiPromptInjectionGuard injectionGuard)
{
    public AiDocumentAnalysis AnalyzeText(AiRequestContext context, AiSource source)
    {
        if (!context.Permissions.Contains("Ai.AnalyzeDocuments") || source.AccountId != context.AccountId || !source.IsAccessible)
            throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(source.Content) || source.Content.Trim().Length < 40)
            return new(false, "Este arquivo não possui texto extraído suficiente para análise automática.", ["OCR não foi executado."], source);
        if (injectionGuard.IsSuspicious(source.Content))
            return new(false, "O conteúdo foi bloqueado pela política de segurança.", ["Possível instrução maliciosa detectada no documento."], source);
        var excerpt = source.Content.Trim();
        if (excerpt.Length > 600) excerpt = excerpt[..600] + "…";
        return new(true, excerpt, ["Resumo baseado somente no texto extraído disponível; confirme os dados no documento original."], source);
    }
}

public sealed record AiDraft(Guid AccountId, Guid UserId, string Type, string Content, bool Sent = false);
public sealed class AiDraftService(IAiRedactionService redaction)
{
    public AiDraft Create(AiRequestContext context, AiGovernancePolicy policy, string type, string content)
    {
        if (context.AccountId != policy.AccountId || !context.Permissions.Contains("Ai.GenerateDrafts") || !policy.AllowCommercialDrafts)
            throw new UnauthorizedAccessException();
        return new(context.AccountId, context.UserId, type, redaction.Sanitize(content));
    }
}
