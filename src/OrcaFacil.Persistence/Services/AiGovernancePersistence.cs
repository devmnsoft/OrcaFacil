using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Ai;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services;

public sealed class AiConsumptionService(OrcaFacilDbContext db, IOptions<AiOptions> options) : IAiConsumptionService
{
    private static readonly string[] CountedStatuses = ["Succeeded", "Failed"];

    public async Task<bool> HasCapacityAsync(Guid accountId, Guid userId, CancellationToken ct = default)
    {
        var limits = options.Value;
        if (limits.MonthlyAccountLimit <= 0 || limits.DailyUserLimit <= 0) return false;
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var dayStart = now.Date;
        var monthly = await db.AiUsageLogs.AsNoTracking().CountAsync(
            x => x.AccountId == accountId && x.CreatedAt >= monthStart && CountedStatuses.Contains(x.Status), ct);
        if (monthly >= limits.MonthlyAccountLimit) return false;
        var daily = await db.AiUsageLogs.AsNoTracking().CountAsync(
            x => x.AccountId == accountId && x.UserId == userId && x.CreatedAt >= dayStart && CountedStatuses.Contains(x.Status), ct);
        return daily < limits.DailyUserLimit;
    }

    public async Task RecordAsync(AiUsageEntry entry, CancellationToken ct = default)
    {
        db.AiUsageLogs.Add(new AiUsageLog
        {
            AccountId = entry.AccountId,
            UserId = entry.UserId,
            OperationType = Trim(entry.OperationType, 80),
            Provider = Trim(entry.Provider, 80),
            Mode = Trim(entry.Mode, 32),
            EstimatedTokens = Math.Max(0, entry.EstimatedTokens),
            EstimatedCost = entry.EstimatedCost < 0 ? 0 : entry.EstimatedCost,
            DurationMs = Math.Max(0, entry.DurationMs),
            Status = Trim(entry.Status, 32),
            SanitizedError = string.IsNullOrWhiteSpace(entry.SanitizedError) ? null : Trim(entry.SanitizedError, 500),
            CorrelationId = Trim(entry.CorrelationId, 100),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private static string Trim(string value, int max) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= max ? value : value[..max];
}

public sealed class AiSuggestionReviewService(OrcaFacilDbContext db) : IAiSuggestionReviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Guid> SavePendingAsync(Guid accountId, Guid userId, BudgetAiSuggestionResult result, CancellationToken ct = default)
    {
        var card = new AiSuggestionCard
        {
            AccountId = accountId,
            DataJson = JsonSerializer.Serialize(new StoredSuggestion(
                "PendingReview",
                userId,
                result.SuggestedScope,
                result.SuggestedNotes,
                result.Notice,
                result.IsRuleBased,
                result.Items.Where(x => x.CatalogItemId.HasValue).Select(x => new StoredItem(
                    x.CatalogItemId!.Value, x.Description, x.Quantity, x.UnitPrice, x.UnitCode)).ToArray()), JsonOptions)
        };
        db.AiSuggestionCards.Add(card);
        await db.SaveChangesAsync(ct);
        return card.Id;
    }

    public async Task<AiBudgetSuggestionReview?> FindAsync(Guid accountId, Guid id, CancellationToken ct = default)
    {
        var card = await db.AiSuggestionCards.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId, ct);
        if (card is null || !TryRead(card.DataJson, out var stored) || stored is null) return null;
        return Map(card.Id, card.AccountId, stored);
    }

    public async Task<bool> MarkAsync(Guid accountId, Guid id, string status, CancellationToken ct = default)
    {
        if (status is not ("Applied" or "Dismissed")) return false;
        var card = await db.AiSuggestionCards.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId, ct);
        if (card is null || !TryRead(card.DataJson, out var stored) || stored is null || stored.Status != "PendingReview") return false;
        card.DataJson = JsonSerializer.Serialize(stored with { Status = status }, JsonOptions);
        card.Touch();
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static bool TryRead(string json, out StoredSuggestion? stored)
    {
        try
        {
            stored = JsonSerializer.Deserialize<StoredSuggestion>(json, JsonOptions);
            return stored is not null;
        }
        catch (JsonException)
        {
            stored = null;
            return false;
        }
    }

    private static AiBudgetSuggestionReview Map(Guid id, Guid accountId, StoredSuggestion stored) =>
        new(id, accountId, stored.Status, stored.Scope, stored.Notes, stored.Notice, stored.IsRuleBased,
            stored.Items.Select(x => new AiBudgetSuggestionItem(x.CatalogItemId, x.Description, x.Quantity, x.UnitPrice, x.UnitCode)).ToArray());

    private sealed record StoredSuggestion(
        string Status,
        Guid UserId,
        string Scope,
        string Notes,
        string Notice,
        bool IsRuleBased,
        StoredItem[] Items);

    private sealed record StoredItem(Guid CatalogItemId, string Description, decimal Quantity, decimal UnitPrice, string UnitCode);
}
