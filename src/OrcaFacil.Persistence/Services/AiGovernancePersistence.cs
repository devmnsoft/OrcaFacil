using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Persistence.Services;

public sealed class AiConsumptionService(OrcaFacilDbContext db, IOptions<AiOptions> options, IPlanAccessService plans) : IAiConsumptionService
{
    private static readonly string[] CountedStatuses = ["Succeeded", "Failed", "Cancelled"];

    public async Task<bool> HasCapacityAsync(Guid accountId, Guid userId, CancellationToken ct = default)
    {
        var limits = await ResolveLimitsAsync(accountId, ct);
        if (limits.DenyReason is not null || limits.Monthly <= 0 || limits.Daily <= 0) return false;
        var monthly = await CountMonthlyAsync(accountId, ct);
        if (monthly >= limits.Monthly) return false;
        var daily = await CountDailyAsync(accountId, userId, ct);
        return daily < limits.Daily;
    }

    public async Task<AiQuotaReservation> TryReserveAsync(Guid accountId, Guid userId, string operationType, string correlationId, CancellationToken ct = default)
    {
        var limits = await ResolveLimitsAsync(accountId, ct);
        if (limits.DenyReason is not null)
            return new(false, limits.DenyReason, 0, 0);
        var correlation = (correlationId ?? string.Empty).Trim();
        var operation = (operationType ?? string.Empty).Trim();
        if (correlation.Length is 0 or > 100 || operation.Length is 0 or > 80)
            return new(false, "A operação inteligente não tem um identificador válido.", null, null);
        if (await AlreadyReservedAsync(accountId, operation, correlation, ct))
            return await BalanceReservationAsync(accountId, userId, true, "A mesma operação já consumiu a cota.", ct);
        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                return await ReserveBucketsAsync(accountId, userId, operation, correlation, limits.Monthly, limits.Daily, ct);
            }
            catch (Exception ex) when (IsUndefinedTable(ex))
            {
                return await ReserveFromLogsAsync(accountId, userId, operation, correlation, ct);
            }
        }
        return await ReserveFromLogsAsync(accountId, userId, operation, correlation, ct);
    }

    public async Task<AiQuotaBalance> GetBalanceAsync(Guid accountId, Guid userId, CancellationToken ct = default)
    {
        var limits = await ResolveLimitsAsync(accountId, ct);
        var monthlyUsed = await CountMonthlyAsync(accountId, ct);
        var dailyUsed = await CountDailyAsync(accountId, userId, ct);
        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var now = DateTime.UtcNow;
                var month = await ReadBucketAsync(accountId, "M:" + now.ToString("yyyyMM"), ct);
                var day = await ReadBucketAsync(accountId, "D:" + userId.ToString("N") + ":" + now.ToString("yyyyMMdd"), ct);
                if (month.HasValue) monthlyUsed = month.Value;
                if (day.HasValue) dailyUsed = day.Value;
            }
            catch (Exception ex) when (IsUndefinedTable(ex))
            {
            }
        }
        string? reason = null;
        if (limits.DenyReason is not null) reason = limits.DenyReason;
        else if (limits.Monthly <= 0 || limits.Daily <= 0) reason = "A cota inteligente está desativada.";
        else if (monthlyUsed >= limits.Monthly) reason = "A cota mensal da conta foi atingida.";
        else if (dailyUsed >= limits.Daily) reason = "A cota diária do usuário foi atingida.";
        return new(limits.Monthly, monthlyUsed, limits.Daily, dailyUsed,
            Remaining(limits.Monthly, monthlyUsed), Remaining(limits.Daily, dailyUsed), reason, true);
    }

    public async Task RecordAsync(AiUsageEntry entry, CancellationToken ct = default)
    {
        var correlation = Trim(entry.CorrelationId, 100);
        var operation = Trim(entry.OperationType, 80);
        if (await db.AiUsageLogs.AsNoTracking().AnyAsync(x => x.AccountId == entry.AccountId && x.OperationType == operation && x.CorrelationId == correlation, ct))
            return;
        db.AiUsageLogs.Add(new AiUsageLog
        {
            AccountId = entry.AccountId,
            UserId = entry.UserId,
            OperationType = operation,
            Provider = Trim(entry.Provider, 80),
            Mode = Trim(entry.Mode, 32),
            EstimatedTokens = Math.Max(0, entry.EstimatedTokens),
            EstimatedCost = entry.EstimatedCost < 0 ? 0 : entry.EstimatedCost,
            DurationMs = Math.Max(0, entry.DurationMs),
            Status = Trim(entry.Status, 32),
            SanitizedError = string.IsNullOrWhiteSpace(entry.SanitizedError) ? null : Trim(entry.SanitizedError, 500),
            CorrelationId = correlation,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            db.ChangeTracker.Clear();
        }
    }

    private async Task<bool> AlreadyReservedAsync(Guid accountId, string operation, string correlation, CancellationToken ct)
    {
        if (await db.AiUsageLogs.AsNoTracking().AnyAsync(x => x.AccountId == accountId && x.OperationType == operation && x.CorrelationId == correlation, ct))
            return true;
        if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true) return false;
        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM orcafacil.ai_quota_reservations WHERE account_id = @account AND correlation_id = @correlation AND operation_type = @operation LIMIT 1";
            Add(command, "@account", accountId);
            Add(command, "@correlation", correlation);
            Add(command, "@operation", operation);
            var found = await command.ExecuteScalarAsync(ct);
            return found is not null && found is not DBNull;
        }
        catch (Exception ex) when (IsUndefinedTable(ex))
        {
            return false;
        }
    }

    private async Task<AiQuotaReservation> ReserveFromLogsAsync(Guid accountId, Guid userId, string operation, string correlation, CancellationToken ct)
    {
        if (await AlreadyReservedAsync(accountId, operation, correlation, ct))
            return await BalanceReservationAsync(accountId, userId, true, "A mesma operação já consumiu a cota.", ct);
        if (!await HasCapacityAsync(accountId, userId, ct))
            return await BalanceReservationAsync(accountId, userId, false, AiQuotaService.LimitMessage, ct);
        return await BalanceReservationAsync(accountId, userId, true, null, ct);
    }

    private async Task<AiQuotaReservation> ReserveBucketsAsync(Guid accountId, Guid userId, string operation, string correlation, int monthlyLimit, int dailyLimit, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthKey = "M:" + now.ToString("yyyyMM");
        var dayKey = "D:" + userId.ToString("N") + ":" + now.ToString("yyyyMMdd");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        var inserted = await ExecuteAsync(connection, transaction,
            """
            INSERT INTO orcafacil.ai_quota_reservations (id, account_id, user_id, correlation_id, operation_type, created_at)
            VALUES (@id, @account, @user, @correlation, @operation, now())
            ON CONFLICT (account_id, correlation_id, operation_type) DO NOTHING
            """,
            ct,
            ("@id", Guid.NewGuid()),
            ("@account", accountId),
            ("@user", userId),
            ("@correlation", correlation),
            ("@operation", operation));
        if (inserted == 0)
        {
            await transaction.CommitAsync(ct);
            return await BalanceReservationAsync(accountId, userId, true, "A mesma operação já consumiu a cota.", ct);
        }
        var monthBaseline = await CountMonthlyAsync(accountId, ct);
        var dayBaseline = await CountDailyAsync(accountId, userId, ct);
        var month = await IncrementBucketAsync(connection, transaction, accountId, monthKey, monthlyLimit, monthBaseline, ct);
        var day = month.HasValue ? await IncrementBucketAsync(connection, transaction, accountId, dayKey, dailyLimit, dayBaseline, ct) : null;
        if (month is null || day is null)
        {
            await transaction.RollbackAsync(ct);
            return await BalanceReservationAsync(accountId, userId, false, AiQuotaService.LimitMessage, ct);
        }
        await transaction.CommitAsync(ct);
        return new(true, null, Math.Max(0, monthlyLimit - month.Value), Math.Max(0, dailyLimit - day.Value));
    }

    private async Task<(int Monthly, int Daily, string? DenyReason)> ResolveLimitsAsync(Guid accountId, CancellationToken ct)
    {
        var configuredMonthly = options.Value.MonthlyAccountLimit;
        var daily = options.Value.DailyUserLimit;
        PlanAccessDecision decision;
        try
        {
            decision = await plans.CanUseAsync(accountId, PlanFeatureCodes.AiMonthlyLimit, ct);
        }
        catch (Exception)
        {
            return (configuredMonthly, daily, "A cota do plano efetivo não pôde ser confirmada.");
        }

        if (decision.InternalReason is "AccountBlocked" or "AccountInactive" or "FeatureNotIncluded" or "PlanLimitReached")
            return (decision.Limit ?? configuredMonthly, daily, decision.UserMessage);

        var monthly = configuredMonthly;
        if (decision.Limit is int planLimit)
            monthly = configuredMonthly <= 0 ? planLimit : Math.Min(configuredMonthly, planLimit);
        if (monthly <= 0 || daily <= 0)
            return (monthly, daily, "A cota inteligente está desativada.");
        return (monthly, daily, null);
    }

    private static async Task<int?> IncrementBucketAsync(DbConnection connection, IDbContextTransaction transaction, Guid accountId, string periodKey, int limit, int baseline, CancellationToken ct)
    {
        if (baseline >= limit) return null;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            INSERT INTO orcafacil.ai_quota_buckets (account_id, period_key, used, updated_at)
            VALUES (@account, @period, @next, now())
            ON CONFLICT (account_id, period_key)
            DO UPDATE SET used = GREATEST(orcafacil.ai_quota_buckets.used, @baseline) + 1, updated_at = now()
            WHERE GREATEST(orcafacil.ai_quota_buckets.used, @baseline) < @limit
            RETURNING used
            """;
        Add(command, "@account", accountId);
        Add(command, "@period", periodKey);
        Add(command, "@next", baseline + 1);
        Add(command, "@baseline", baseline);
        Add(command, "@limit", limit);
        var value = await command.ExecuteScalarAsync(ct);
        return value is null || value is DBNull ? null : Convert.ToInt32(value);
    }

    private async Task<int?> ReadBucketAsync(Guid accountId, string periodKey, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT used FROM orcafacil.ai_quota_buckets WHERE account_id = @account AND period_key = @period";
        Add(command, "@account", accountId);
        Add(command, "@period", periodKey);
        var value = await command.ExecuteScalarAsync(ct);
        return value is null || value is DBNull ? null : Convert.ToInt32(value);
    }

    private static async Task<int> ExecuteAsync(DbConnection connection, IDbContextTransaction transaction, string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Value);
        return await command.ExecuteNonQueryAsync(ct);
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private async Task<AiQuotaReservation> BalanceReservationAsync(Guid accountId, Guid userId, bool allowed, string? reason, CancellationToken ct)
    {
        var balance = await GetBalanceAsync(accountId, userId, ct);
        return new(allowed, allowed ? reason : balance.BlockReason ?? reason, balance.MonthlyRemaining, balance.DailyRemaining);
    }

    private async Task<int> CountMonthlyAsync(Guid accountId, CancellationToken ct)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await db.AiUsageLogs.AsNoTracking().CountAsync(x => x.AccountId == accountId && x.CreatedAt >= monthStart && CountedStatuses.Contains(x.Status), ct);
    }

    private async Task<int> CountDailyAsync(Guid accountId, Guid userId, CancellationToken ct)
    {
        var dayStart = DateTime.UtcNow.Date;
        return await db.AiUsageLogs.AsNoTracking().CountAsync(
            x => x.AccountId == accountId && x.UserId == userId && x.CreatedAt >= dayStart && CountedStatuses.Contains(x.Status), ct);
    }

    private static int? Remaining(int limit, int used) => limit <= 0 ? 0 : Math.Max(0, limit - used);

    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var state = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
            if (state == "23505") return true;
        }
        return false;
    }

    private static bool IsUndefinedTable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var state = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
            if (state == "42P01") return true;
        }
        return false;
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
            Status = "PendingReview",
            DataJson = JsonSerializer.Serialize(new StoredSuggestion(
                userId,
                result.SuggestedScope,
                result.SuggestedNotes,
                result.Notice,
                result.IsRuleBased,
                result.Items.Where(x => x.CatalogItemId.HasValue).Select(x => new StoredItem(
                    x.CatalogItemId!.Value, x.Description, x.Quantity, x.UnitPrice, x.UnitCode)).ToArray()), JsonOptions)
        };
        card.Touch();
        db.AiSuggestionCards.Add(card);
        await db.SaveChangesAsync(ct);
        return card.Id;
    }

    public async Task<AiBudgetSuggestionReview?> FindAsync(Guid accountId, Guid id, CancellationToken ct = default)
    {
        var card = await db.AiSuggestionCards.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId, ct);
        if (card is null || !TryRead(card.DataJson, out var stored) || stored is null) return null;
        return Map(card, stored);
    }

    public async Task<IReadOnlyList<AiBudgetSuggestionReview>> ListPendingAsync(Guid accountId, int take, CancellationToken ct = default)
    {
        var cards = await db.AiSuggestionCards.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Status == "PendingReview")
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 20))
            .ToListAsync(ct);
        var mapped = new List<AiBudgetSuggestionReview>(cards.Count);
        foreach (var card in cards)
            if (TryRead(card.DataJson, out var stored) && stored is not null)
                mapped.Add(Map(card, stored));
        return mapped;
    }

    public async Task<bool> MarkAsync(Guid accountId, Guid id, string status, CancellationToken ct = default)
    {
        if (status is not ("Applied" or "Dismissed")) return false;
        var now = DateTime.UtcNow;
        var updated = await db.AiSuggestionCards
            .Where(x => x.Id == id && x.AccountId == accountId && x.Status == "PendingReview")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.UpdatedAt, now), ct);
        return updated == 1;
    }

    public async Task<bool> TryMarkAppliedAsync(Guid accountId, Guid id, string applyFingerprint, Guid documentId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var updated = await db.AiSuggestionCards
            .Where(x => x.Id == id && x.AccountId == accountId && x.Status == "PendingReview")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "Applied")
                .SetProperty(x => x.AppliedDocumentId, documentId)
                .SetProperty(x => x.ApplyFingerprint, applyFingerprint)
                .SetProperty(x => x.AppliedAt, now)
                .SetProperty(x => x.UpdatedAt, now), ct);
        return updated == 1;
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

    private static AiBudgetSuggestionReview Map(AiSuggestionCard card, StoredSuggestion stored) =>
        new(card.Id, card.AccountId, card.Status, stored.Scope, stored.Notes, stored.Notice, stored.IsRuleBased,
            stored.Items.Select(x => new AiBudgetSuggestionItem(x.CatalogItemId, x.Description, x.Quantity, x.UnitPrice, x.UnitCode)).ToArray(),
            card.AppliedDocumentId, card.ApplyFingerprint);

    private sealed record StoredSuggestion(
        Guid UserId,
        string Scope,
        string Notes,
        string Notice,
        bool IsRuleBased,
        StoredItem[] Items);

    private sealed record StoredItem(Guid CatalogItemId, string Description, decimal Quantity, decimal UnitPrice, string UnitCode);
}
