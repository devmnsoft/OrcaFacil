using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Jobs;

public interface IJobLockService
{
    Task<bool> TryAcquireAsync(string name, string instanceId, TimeSpan lease, CancellationToken cancellationToken = default);
    Task ReleaseAsync(string name, string instanceId, CancellationToken cancellationToken = default);
}

public interface IProcessingOutboxService
{
    Task<ProcessingOutboxItem> EnqueueAsync(Guid accountId, string type, string idempotencyKey, string payloadJson, int priority = 0, int maximumAttempts = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessingOutboxItem>> ClaimAsync(string instanceId, int batchSize, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task FailAsync(Guid id, string safeError, CancellationToken cancellationToken = default);
    Task<bool> RequeueFailedAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record QuotaDecision(bool Allowed, bool NearLimit, long Used, long Limit, string Message);

public sealed class QuotaService
{
    public QuotaDecision CheckCreation(string resourceDisplayName, long used, long limit)
    {
        if (limit < 0) return new(true, false, used, limit, string.Empty);
        var allowed = used < limit;
        var near = limit > 0 && used * 100 >= limit * 80;
        var message = allowed ? (near ? $"Você já utilizou {used} de {limit} {resourceDisplayName}." : string.Empty)
            : $"Você atingiu o limite de {resourceDisplayName} do seu plano. Para continuar, faça upgrade ou fale com o suporte.";
        return new(allowed, near, used, limit, message);
    }
}

public static class TenantCacheKey
{
    public static string Create(Guid accountId, string region, string key)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId é obrigatório para cache de conta.", nameof(accountId));
        return $"tenant:{accountId:N}:{region}:{key}";
    }
}
