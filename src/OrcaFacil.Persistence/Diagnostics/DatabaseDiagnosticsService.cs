using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
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
        "plans", "plan_versions"
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
            await connection.OpenAsync(ct);

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

            var freePlan = missing.Length == 0 && await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from orcafacil.plans where code='FREE' and is_active and not is_deleted)", cancellationToken: ct));
            var publishedFreeVersion = freePlan && await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                """
                select exists(select 1 from orcafacil.plan_versions pv join orcafacil.plans p on p.id=pv.plan_id
                 where p.code='FREE' and pv.status='Published' and not pv.is_deleted
                   and pv.valid_from <= now() and (pv.valid_until is null or pv.valid_until > now()))
                """, cancellationToken: ct));

            return new(true, schemaExists, existingTables, missing, databaseName, version, null, freePlan, publishedFreeVersion);
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
