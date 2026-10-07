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

public interface IAiConsumptionService
{
    Task<bool> HasCapacityAsync(Guid accountId, Guid userId, CancellationToken ct = default);
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
    IReadOnlyList<AiBudgetSuggestionItem> Items);

public interface IAiSuggestionReviewService
{
    Task<Guid> SavePendingAsync(Guid accountId, Guid userId, BudgetAiSuggestionResult result, CancellationToken ct = default);
    Task<AiBudgetSuggestionReview?> FindAsync(Guid accountId, Guid id, CancellationToken ct = default);
    Task<bool> MarkAsync(Guid accountId, Guid id, string status, CancellationToken ct = default);
}
