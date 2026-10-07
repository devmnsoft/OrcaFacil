namespace OrcaFacil.Application.Ai;

public sealed record AiUsageEntry(
    Guid AccountId,
    Guid UserId,
    string OperationType,
    string Provider,
    string Mode,
    int EstimatedTokens,
    decimal EstimatedCost,
    int DurationMs,
    string Status,
    string? SanitizedError,
    string CorrelationId);

public sealed record AiQuotaReservation(bool Allowed, string? Reason, int? MonthlyRemaining, int? DailyRemaining);
public sealed record AiQuotaBalance(int MonthlyLimit, int MonthlyUsed, int DailyLimit, int DailyUsed, int? MonthlyRemaining, int? DailyRemaining, string? BlockReason, bool Estimated);

public interface IAiConsumptionService
{
    Task<bool> HasCapacityAsync(Guid accountId, Guid userId, CancellationToken ct = default);
    Task<AiQuotaReservation> TryReserveAsync(Guid accountId, Guid userId, string operationType, string correlationId, CancellationToken ct = default);
    Task<AiQuotaBalance> GetBalanceAsync(Guid accountId, Guid userId, CancellationToken ct = default);
    Task RecordAsync(AiUsageEntry entry, CancellationToken ct = default);
}

public sealed record AiBudgetSuggestionItem(
    Guid CatalogItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string UnitCode);

public sealed record AiBudgetSuggestionReview(
    Guid Id,
    Guid AccountId,
    string Status,
    string Scope,
    string Notes,
    string Notice,
    bool IsRuleBased,
    IReadOnlyList<AiBudgetSuggestionItem> Items,
    Guid? AppliedDocumentId = null,
    string? ApplyFingerprint = null);

public interface IAiSuggestionReviewService
{
    Task<Guid> SavePendingAsync(Guid accountId, Guid userId, BudgetAiSuggestionResult result, CancellationToken ct = default);
    Task<AiBudgetSuggestionReview?> FindAsync(Guid accountId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AiBudgetSuggestionReview>> ListPendingAsync(Guid accountId, int take, CancellationToken ct = default);
    Task<bool> MarkAsync(Guid accountId, Guid id, string status, CancellationToken ct = default);

    /// <summary>
    /// Marca a revisão como aplicada somente se ainda estiver pendente (compare-and-set no banco).
    /// Deve ser chamada dentro da transação ambiente que persiste o documento, garantindo
    /// atomicidade entre a gravação do documento e a transição da revisão.
    /// Retorna false quando outra operação já transitou a revisão.
    /// </summary>
    Task<bool> TryMarkAppliedAsync(Guid accountId, Guid id, string applyFingerprint, Guid documentId, CancellationToken ct = default);
}
