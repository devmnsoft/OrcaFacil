using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Diagnostics;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Persistence.Diagnostics;

public sealed class DatabaseDiagnosticsService : IDatabaseDiagnosticsService
{
    public const string ExpectedSchema = "orcafacil";

    public static readonly IReadOnlyList<string> RequiredTables =
    [
        "users", "issuer_profiles", "documents", "document_items", "public_quotes",
        "user_usage", "subscriptions", "payments", "payment_events", "mercadopago_webhook_events",
        "billing_customer_profiles", "clients", "plan_features", "admin_settings", "notifications",
        "audit_logs", "system_logs", "system_errors", "business_accounts", "account_members",
        "plans", "plan_versions", "account_onboarding_states"
    ];

    private readonly IConfiguration _configuration;

    public DatabaseDiagnosticsService(IConfiguration configuration) => _configuration = configuration;

    public async Task<bool> CanConnectForUserActionAsync(CancellationToken ct = default)
    {
        var result = await CheckAsync(ct);
        return result.CanConnect;
    }

    public async Task<DatabaseDiagnosticsDto> CheckAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new(false, false, [], RequiredTables, null, null, "Connection string DefaultConnection ausente.");
        }

        string? databaseName = null;
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            databaseName = builder.Database;

            await using var connection = new NpgsqlConnection(connectionString);
            var stopwatch = Stopwatch.StartNew();
            await connection.OpenAsync(ct);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition("select 1", cancellationToken: ct));
            stopwatch.Stop();

            var schemaExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from information_schema.schemata where schema_name = @Schema)",
                new { Schema = ExpectedSchema }, cancellationToken: ct));

            var existingTables = (await connection.QueryAsync<string>(new CommandDefinition(
                """
                select table_name
                  from information_schema.tables
                 where table_schema = @Schema
                   and table_type = 'BASE TABLE'
                 order by table_name
                """,
                new { Schema = ExpectedSchema }, cancellationToken: ct))).AsList();

            var existing = existingTables.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = RequiredTables.Where(table => !existing.Contains(table)).OrderBy(table => table).ToArray();

            var version = await connection.ExecuteScalarAsync<string>(new CommandDefinition(
                "select version()", cancellationToken: ct));
            var connectedUser = await connection.ExecuteScalarAsync<string>(new CommandDefinition("select current_user", cancellationToken: ct));
            var searchPath = await connection.ExecuteScalarAsync<string>(new CommandDefinition("show search_path", cancellationToken: ct));
            var canRead = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("select has_schema_privilege(current_user, @Schema, 'USAGE')", new { Schema = ExpectedSchema }, cancellationToken: ct));
            var canWrite = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("select has_schema_privilege(current_user, @Schema, 'CREATE')", new { Schema = ExpectedSchema }, cancellationToken: ct));
            // Keep authentication prerequisites explicit. A partially upgraded users table must be
            // diagnosed before EF Core attempts to materialize UserAccount during sign-in.
            var requiredColumns = new[]
            {
                "documents.account_id", "clients.account_id", "public_document_accesses.token_hash",
                "users.failed_login_attempts", "users.last_failed_login_at",
                "users.last_successful_login_at", "users.locked_until", "users.is_blocked",
                "users.block_reason", "users.must_change_password", "users.password_changed_at",
                "users.password_changed_by_user_id", "users.password_expires_at",
                "users.password_reset_reason", "users.session_version", "users.accepted_terms_at",
                "users.accepted_privacy_at", "users.legacy_unversioned_acceptance",
                "account_onboarding_states.id", "account_onboarding_states.account_id",
                "account_onboarding_states.user_id", "account_onboarding_states.current_step",
                "account_onboarding_states.completed_at", "account_onboarding_states.skipped_at",
                "account_onboarding_states.last_seen_at", "account_onboarding_states.created_at",
                "account_onboarding_states.updated_at", "account_onboarding_states.is_deleted"
            };
            var columns = (await connection.QueryAsync<string>(new CommandDefinition("select table_name || '.' || column_name from information_schema.columns where table_schema=@Schema", new { Schema = ExpectedSchema }, cancellationToken: ct))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingColumns = requiredColumns.Where(x => !columns.Contains(x)).ToArray();
            var requiredIndexes = new[]
            {
                "ix_documents_account_client", "ux_public_document_access_token_hash", "ix_work_orders_schedule",
                "ix_account_onboarding_states_account_id_user_id",
                "ix_account_onboarding_states_current_step_last_seen_at"
            };
            var indexes = (await connection.QueryAsync<string>(new CommandDefinition("select indexname from pg_indexes where schemaname=@Schema", new { Schema = ExpectedSchema }, cancellationToken: ct))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingIndexes = requiredIndexes.Where(x => !indexes.Contains(x)).ToArray();
            var appliedMigrations = existing.Contains("__EFMigrationsHistory")
                ? (await connection.QueryAsync<string>(new CommandDefinition("select migration_id from orcafacil.\"__EFMigrationsHistory\" order by migration_id", cancellationToken: ct))).ToArray()
                : Array.Empty<string>();

            var freePlan = missing.Length == 0 && await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from orcafacil.plans where code='FREE' and is_active and not is_deleted)", cancellationToken: ct));
            var publishedFreeVersion = freePlan && await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                """
                select exists(select 1 from orcafacil.plan_versions pv join orcafacil.plans p on p.id=pv.plan_id
                 where p.code='FREE' and pv.status='Published' and not pv.is_deleted
                   and pv.valid_from <= now() and (pv.valid_until is null or pv.valid_until > now()))
                """, cancellationToken: ct));

            return new(true, schemaExists, existingTables, missing, databaseName, version, null, freePlan, publishedFreeVersion,
                missingColumns, missingIndexes, connectedUser, searchPath, stopwatch.ElapsedMilliseconds, canRead, canWrite, appliedMigrations);
        }
        catch (Exception ex)
        {
            return new(false, false, [], RequiredTables, databaseName, null, SanitizeError(ex));
        }
    }

    public static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password)) builder.Password = "******";
        return builder.ConnectionString;
    }

    private static string SanitizeError(Exception ex)
    {
        if (HasSqlState(ex, "28P01"))
        {
            return PostgresFailureClassifier.Classify(ex).AdminMessage;
        }

        return PostgresFailureClassifier.Classify(ex).AdminMessage;
    }

    private static bool HasSqlState(Exception ex, string sqlState)
    {
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            var value = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (string.Equals(value, sqlState, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
