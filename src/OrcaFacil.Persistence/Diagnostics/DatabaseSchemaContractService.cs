using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Persistence.Diagnostics;

public sealed class DatabaseSchemaContractService(IConfiguration configuration) : IDatabaseSchemaContractService
{
    public const string RepairMigration = "20260728210000_RepairBillingCustomerProfileSchema";

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> RegistrationContract =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["users"] = Columns(("id", "uuid"), ("email", "character varying"), ("password_hash", "character varying"), ("session_version", "integer")),
            ["business_accounts"] = Columns(("id", "uuid"), ("document_number", "character varying"), ("is_deleted", "boolean")),
            ["account_members"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid"), ("role_code", "character varying")),
            ["billing_customer_profiles"] = Columns(
                ("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid"), ("person_type", "character varying"),
                ("document_type", "character varying"), ("document_number", "character varying"), ("name", "character varying"),
                ("trade_name", "character varying"), ("legal_name", "character varying"), ("email", "character varying"),
                ("phone", "character varying"), ("city", "character varying"), ("state", "character varying"),
                ("postal_code", "character varying"), ("street", "character varying"), ("street_number", "character varying"),
                ("complement", "character varying"), ("district", "character varying"), ("address", "character varying"),
                ("mercado_pago_customer_id", "character varying"), ("created_at", "timestamp with time zone"),
                ("updated_at", "timestamp with time zone"), ("is_deleted", "boolean")),
            ["subscriptions"] = Columns(("id", "uuid"), ("account_id", "uuid"), ("user_id", "uuid")),
            ["issuer_profiles"] = Columns(("id", "uuid"), ("user_id", "uuid")),
            ["notifications"] = Columns(("id", "uuid"), ("user_id", "uuid"), ("account_id", "uuid")),
            ["audit_logs"] = Columns(("id", "uuid"), ("user_id", "uuid"), ("account_id", "uuid")),
            ["plans"] = Columns(("id", "uuid"), ("code", "character varying")),
            ["plan_versions"] = Columns(("id", "uuid"), ("plan_id", "uuid"), ("status", "character varying"))
        };

    public async Task<DatabaseSchemaContractResult> CheckRegistrationContractAsync(CancellationToken ct = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return Failed("database", null, "ConnectionNotConfigured");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        var columns = (await connection.QueryAsync<ColumnInfo>(new CommandDefinition("""
            SELECT table_name AS TableName, column_name AS ColumnName, data_type AS DataType
              FROM information_schema.columns
             WHERE table_schema = 'orcafacil'
               AND table_name = ANY(@Tables)
            """, new { Tables = RegistrationContract.Keys.ToArray() }, cancellationToken: ct))).AsList();

        var actual = columns.ToDictionary(x => $"{x.TableName}.{x.ColumnName}", StringComparer.OrdinalIgnoreCase);
        var issues = new List<DatabaseSchemaContractIssue>();
        foreach (var (table, expectedColumns) in RegistrationContract)
        foreach (var (column, type) in expectedColumns)
        {
            if (!actual.TryGetValue($"{table}.{column}", out var found))
                issues.Add(new(table, column, "Missing", RepairMigration));
            else if (!string.Equals(found.DataType, type, StringComparison.OrdinalIgnoreCase))
                issues.Add(new(table, column, $"TypeMismatch ({found.DataType})", RepairMigration));
        }

        await CheckEssentialObjectAsync(connection, issues, "billing_customer_profiles", "uq_billing_profiles_account_id", "IndexMissing", ct);
        await CheckEssentialObjectAsync(connection, issues, "account_members", "account_members_account_id_fkey", "ConstraintMissing", ct, constraint: true);

        var historySchema = await connection.ExecuteScalarAsync<string?>(new CommandDefinition("""
            select table_schema from information_schema.tables
             where table_name = '__EFMigrationsHistory' and table_schema in ('orcafacil', 'public')
             order by case when table_schema = 'orcafacil' then 0 else 1 end limit 1
            """, cancellationToken: ct));
        var trustedHistorySchema = historySchema == "orcafacil" ? "orcafacil" : historySchema == "public" ? "public" : null;
        var migrationApplied = trustedHistorySchema is not null && await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            $"select exists(select 1 from \"{trustedHistorySchema}\".\"__EFMigrationsHistory\" where \"MigrationId\" = @Migration)",
            new { Migration = RepairMigration }, cancellationToken: ct));
        var pending = !migrationApplied;
        return new(issues.Count == 0 && !pending, issues, DateTimeOffset.UtcNow, pending);
    }

    private static async Task CheckEssentialObjectAsync(NpgsqlConnection connection, List<DatabaseSchemaContractIssue> issues,
        string table, string name, string state, CancellationToken ct, bool constraint = false)
    {
        var sql = constraint
            ? "select exists(select 1 from information_schema.table_constraints where table_schema='orcafacil' and table_name=@Table and constraint_name=@Name)"
            : "select exists(select 1 from pg_indexes where schemaname='orcafacil' and tablename=@Table and indexname=@Name)";
        if (!await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { Table = table, Name = name }, cancellationToken: ct)))
            issues.Add(new(table, null, state, RepairMigration));
    }

    private static IReadOnlyDictionary<string, string> Columns(params (string Name, string Type)[] columns) =>
        columns.ToDictionary(x => x.Name, x => x.Type, StringComparer.OrdinalIgnoreCase);

    private static DatabaseSchemaContractResult Failed(string table, string? column, string state) =>
        new(false, [new(table, column, state, RepairMigration)], DateTimeOffset.UtcNow, true);

    private sealed record ColumnInfo(string TableName, string ColumnName, string DataType);
}
