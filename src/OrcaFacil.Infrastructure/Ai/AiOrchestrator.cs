using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Ai;

namespace OrcaFacil.Infrastructure.Ai;

public sealed class AiOrchestrator : IAiOrchestrator
{
    private readonly IEnumerable<IAiModelClient> _clients;
    private readonly AiOptions _options;
    private readonly AiCircuitBreaker _circuitBreaker;
    private readonly ILogger<AiOrchestrator> _logger;
    private readonly IAiRedactionService _redaction;
    private readonly AiPromptInjectionGuard _injection;
    private readonly IAiConsumptionService? _consumption;
    private readonly IServiceScopeFactory? _scopes;
    private readonly ConcurrentDictionary<Guid, AccountRateLimiter> _rateLimits = new();

    public AiOrchestrator(
        IEnumerable<IAiModelClient> clients,
        IOptions<AiOptions> options,
        AiCircuitBreaker circuitBreaker,
        ILogger<AiOrchestrator> logger,
        IAiRedactionService? redaction = null,
        AiPromptInjectionGuard? injectionGuard = null,
        IAiConsumptionService? consumption = null,
        IServiceScopeFactory? scopes = null)
    {
        _clients = clients;
        _options = options.Value;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
        _redaction = redaction ?? new AiRedactionService();
        _injection = injectionGuard ?? new AiPromptInjectionGuard();
        _consumption = consumption;
        _scopes = scopes;
    }

    public async Task<AiExecutionResult> ExecuteAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string purpose,
        AiClientRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        if (context.AccountId == Guid.Empty || context.AccountId != policy.AccountId)
        {
            return FallbackToRules("Conta não identificada.");
        }

        if (!policy.AccountActive || !policy.FeatureEnabled || policy.AllowAutomaticCriticalActions)
        {
            return FallbackToRules("A conta ou o recurso inteligente não está autorizado para esta operação.");
        }

        if (AiActionPolicy.Prohibited.Contains(purpose) || !HasPurposePermission(context, purpose))
        {
            return FallbackToRules("Você não tem permissão para usar este recurso inteligente.");
        }

        if (purpose.Equals("budget_assistant", StringComparison.OrdinalIgnoreCase) && !policy.AllowSuggestions)
        {
            return FallbackToRules("As sugestões inteligentes estão desativadas para esta conta.");
        }

        var prompt = _redaction.Sanitize(request.Prompt);
        var systemPrompt = string.IsNullOrWhiteSpace(request.SystemPrompt) ? null : _redaction.Sanitize(request.SystemPrompt);
        if (_injection.IsSuspicious(prompt) || _injection.IsSuspicious(systemPrompt))
        {
            _logger.LogWarning("AI_PROMPT_BLOCKED AccountId {AccountId} Purpose {Purpose}", context.AccountId, purpose);
            return FallbackToRules("O pedido foi bloqueado pela política de segurança. Nenhum provedor externo foi chamado.");
        }

        var inputChars = prompt.Length + (systemPrompt?.Length ?? 0);
        if (_options.MaxInputTokens > 0 && inputChars > _options.MaxInputTokens * 4)
        {
            return FallbackToRules("O pedido excede o limite de entrada configurado. Nenhum provedor externo foi chamado.");
        }

        var maxTokens = Math.Clamp(request.MaxTokens ?? _options.MaxOutputTokens, 1, Math.Max(1, _options.MaxOutputTokens));
        var safeRequest = request with { Prompt = prompt, SystemPrompt = systemPrompt, MaxTokens = maxTokens };

        var limiter = _rateLimits.GetOrAdd(context.AccountId, _ => new AccountRateLimiter());
        if (!limiter.Allow(_options.RateLimitPerMinute))
        {
            _logger.LogWarning("AI_RATE_LIMIT_EXCEEDED AccountId {AccountId}", context.AccountId);
            return FallbackToRules("Limite de requisições por minuto excedido para esta conta. Tente novamente em instantes.");
        }

        var candidates = _clients.Where(x => x.IsConfigured).ToList();
        if (candidates.Count == 0)
        {
            return FallbackToRules("Nenhum provedor externo está habilitado ou com credenciais configuradas.");
        }

        var ordered = OrderProviders(candidates, preferredProvider, _options.DefaultProvider)
            .Where(x => !IsDenied(policy, x.ProviderName) && _circuitBreaker.CanAttempt(x.ProviderName))
            .ToList();
        if (ordered.Count == 0)
        {
            return FallbackToRules("Os provedores configurados estão temporariamente indisponíveis após falhas consecutivas.");
        }

        var correlation = string.IsNullOrWhiteSpace(context.CorrelationId) ? Guid.NewGuid().ToString("N") : context.CorrelationId.Trim();
        var reservation = await ReserveAsync(context, purpose, correlation, ct);
        if (reservation is not null && !reservation.Allowed)
            return FallbackToRules(reservation.Reason ?? AiQuotaService.LimitMessage);

        string? lastError = null;
        foreach (var chosen in ordered)
        {
            var response = await chosen.ExecuteChatAsync(safeRequest, ct);
            if (response.Succeeded)
            {
                _circuitBreaker.RecordSuccess(chosen.ProviderName);
                var content = _redaction.Sanitize(response.Content);
                await RecordAsync(context, purpose, chosen.ProviderName, "ExternalProvider", "Succeeded",
                    response.PromptTokens + response.CompletionTokens, response.LatencyMs, null, correlation, ct);
                return new AiExecutionResult(
                    true,
                    content,
                    AiOperatingMode.ExternalProvider,
                    chosen.ProviderName,
                    response.Model,
                    response.PromptTokens,
                    response.CompletionTokens,
                    response.LatencyMs,
                    false);
            }

            if (response.ErrorCode is not ("model_not_allowed" or "provider_not_configured" or "empty_response" or "blocked_response" or "truncated_response" or "invalid_json"))
                _circuitBreaker.RecordFailure(chosen.ProviderName);
            lastError = _redaction.Sanitize(response.ErrorMessage);
            _logger.LogWarning("AI_EXECUTION_FAILED Provider {Provider} Code {Code}", chosen.ProviderName, response.ErrorCode);
        }

        await RecordAsync(context, purpose, ordered[0].ProviderName, "ExternalProvider", "Failed", 0, 0, lastError, correlation, ct);
        return FallbackToRules(string.IsNullOrWhiteSpace(lastError) ? "Falha na resposta do provedor de IA." : lastError);
    }

    private async Task<AiQuotaReservation?> ReserveAsync(AiRequestContext context, string purpose, string correlation, CancellationToken ct)
    {
        try
        {
            if (_scopes is not null)
            {
                using var scope = _scopes.CreateScope();
                var consumption = scope.ServiceProvider.GetService<IAiConsumptionService>();
                if (consumption is null) return null;
                return await consumption.TryReserveAsync(context.AccountId, context.UserId, purpose, correlation, ct);
            }

            if (_consumption is null) return null;
            return await _consumption.TryReserveAsync(context.AccountId, context.UserId, purpose, correlation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AI_QUOTA_CHECK_FAILED AccountId {AccountId} Type {ExceptionType}", context.AccountId, ex.GetType().Name);
            return new AiQuotaReservation(false, AiQuotaService.LimitMessage, 0, 0);
        }
    }

    private async Task RecordAsync(AiRequestContext context, string purpose, string provider, string mode, string status,
        int tokens, long latencyMs, string? error, string correlation, CancellationToken ct)
    {
        try
        {
            var entry = new AiUsageEntry(
                context.AccountId,
                context.UserId,
                purpose,
                provider,
                mode,
                tokens,
                0m,
                (int)Math.Min(int.MaxValue, Math.Max(0, latencyMs)),
                status,
                error,
                correlation);
            if (_scopes is not null)
            {
                using var scope = _scopes.CreateScope();
                var consumption = scope.ServiceProvider.GetService<IAiConsumptionService>();
                if (consumption is null) return;
                await consumption.RecordAsync(entry, ct);
                return;
            }

            if (_consumption is null) return;
            await _consumption.RecordAsync(entry, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AI_USAGE_RECORD_FAILED AccountId {AccountId} Type {ExceptionType}", context.AccountId, ex.GetType().Name);
        }
    }

    private static IEnumerable<IAiModelClient> OrderProviders(IReadOnlyList<IAiModelClient> candidates, string? preferred, string? configuredDefault)
    {
        var ordered = new List<IAiModelClient>();
        void Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var match = candidates.FirstOrDefault(x => x.ProviderName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match is not null && !ordered.Contains(match)) ordered.Add(match);
        }

        Add(preferred);
        Add(configuredDefault);
        foreach (var name in new[] { "Groq", "Gemini", "DeepSeek" }) Add(name);
        foreach (var candidate in candidates)
        {
            if (!ordered.Contains(candidate)) ordered.Add(candidate);
        }

        return ordered;
    }

    private static bool HasPurposePermission(AiRequestContext context, string purpose)
    {
        if (AiActionPolicy.Prohibited.Contains(purpose)) return false;
        var permissions = context.Permissions;
        return permissions.Contains("Ai.Suggest")
            || permissions.Contains("Ai.ApplySuggestions")
            || permissions.Contains("Ai.GenerateDrafts")
            || permissions.Contains("documents.create");
    }

    private static bool IsDenied(AiGovernancePolicy policy, string provider) =>
        policy.DeniedProviders?.Any(x => x.Equals(provider, StringComparison.OrdinalIgnoreCase)) == true;

    private static AiExecutionResult FallbackToRules(string message) =>
        new(false, string.Empty, AiOperatingMode.RulesOnly, "Rules", string.Empty, 0, 0, 0, true, message);

    private sealed class AccountRateLimiter
    {
        private readonly List<DateTime> _timestamps = [];
        private readonly object _lock = new();

        public bool Allow(int limitPerMinute)
        {
            var now = DateTime.UtcNow;
            lock (_lock)
            {
                _timestamps.RemoveAll(t => t < now.AddMinutes(-1));
                if (_timestamps.Count >= limitPerMinute)
                    return false;

                _timestamps.Add(now);
                return true;
            }
        }
    }
}
