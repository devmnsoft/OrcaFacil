using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Guid, AccountRateLimiter> _rateLimits = new();

    public AiOrchestrator(
        IEnumerable<IAiModelClient> clients,
        IOptions<AiOptions> options,
        AiCircuitBreaker circuitBreaker,
        ILogger<AiOrchestrator> logger)
    {
        _clients = clients;
        _options = options.Value;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    public async Task<AiExecutionResult> ExecuteAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string purpose,
        AiClientRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        if (context.AccountId == Guid.Empty)
        {
            return FallbackToRules("Conta não identificada.");
        }

        // Rate limiting check
        var limiter = _rateLimits.GetOrAdd(context.AccountId, _ => new AccountRateLimiter());
        if (!limiter.Allow(_options.RateLimitPerMinute))
        {
            _logger.LogWarning("AI_RATE_LIMIT_EXCEEDED AccountId {AccountId}", context.AccountId);
            return FallbackToRules("Limite de requisições por minuto excedido para esta conta. Tente novamente em instantes.");
        }

        var providerName = preferredProvider;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            providerName = _options.DefaultProvider;
        }

        var candidates = _clients.Where(x => x.IsConfigured).ToList();
        if (candidates.Count == 0)
        {
            return FallbackToRules("Nenhum provedor externo está habilitado ou com credenciais configuradas.");
        }

        IAiModelClient? chosen = null;
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            chosen = candidates.FirstOrDefault(x => x.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        }

        chosen ??= candidates.FirstOrDefault();
        if (chosen == null)
        {
            return FallbackToRules("Provedor solicitado indisponível.");
        }

        if (!_circuitBreaker.CanAttempt(chosen.ProviderName))
        {
            _logger.LogWarning("AI_CIRCUIT_BREAKER_OPEN Provider {Provider}", chosen.ProviderName);
            // Check fallback provider
            var fallback = candidates.FirstOrDefault(x => !x.ProviderName.Equals(chosen.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                                                          _circuitBreaker.CanAttempt(x.ProviderName));
            if (fallback != null)
            {
                _logger.LogInformation("AI_FALLBACK_TRIGGERED From {From} To {To}", chosen.ProviderName, fallback.ProviderName);
                chosen = fallback;
            }
            else
            {
                return FallbackToRules($"Provedor {chosen.ProviderName} temporariamente indisponível após falhas consecutivas.");
            }
        }

        var response = await chosen.ExecuteChatAsync(request, ct);
        if (response.Succeeded)
        {
            _circuitBreaker.RecordSuccess(chosen.ProviderName);
            return new AiExecutionResult(
                true,
                response.Content,
                AiOperatingMode.ExternalProvider,
                chosen.ProviderName,
                response.Model,
                response.PromptTokens,
                response.CompletionTokens,
                response.LatencyMs,
                false);
        }

        _circuitBreaker.RecordFailure(chosen.ProviderName);
        _logger.LogWarning("AI_EXECUTION_FAILED Provider {Provider} Error {Error}", chosen.ProviderName, response.ErrorMessage);

        return FallbackToRules(response.ErrorMessage ?? "Falha na resposta do provedor de IA.");
    }

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
