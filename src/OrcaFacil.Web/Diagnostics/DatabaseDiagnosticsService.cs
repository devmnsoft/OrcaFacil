using System.Reflection;
using Npgsql;

namespace OrcaFacil.Web.Diagnostics;

public sealed record DatabaseDiagnosticsResult(
    bool Connected,
    bool SchemaFound,
    int TableCount,
    IReadOnlyDictionary<string, bool> RequiredTables,
    string? LastError,
    string Database,
    string MaskedConnectionString,
    string AppVersion,
    IReadOnlyList<string> AppliedMigrations);

public sealed class DatabaseDiagnosticsService
{
    public const string ExpectedSchema = "orcafacil";
    public static readonly string[] RequiredTableNames = ["users", "documents", "document_items", "admin_settings", "system_errors"];
    private readonly IConfiguration _configuration;

    public DatabaseDiagnosticsService(IConfiguration configuration) => _configuration = configuration;

    public async Task<DatabaseDiagnosticsResult> CheckAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var builder = string.IsNullOrWhiteSpace(connectionString) ? null : new NpgsqlConnectionStringBuilder(connectionString);
        var required = RequiredTableNames.ToDictionary(x => x, _ => false);
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        if (builder is null)
        {
            return new(false, false, 0, required, "Connection string ausente.", string.Empty, string.Empty, version, []);
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(ct);
            var schemaFound = await ScalarAsync<bool>(connection, "select exists(select 1 from information_schema.schemata where schema_name = @schema)", new NpgsqlParameter("schema", ExpectedSchema), ct);
            var tableCount = await ScalarAsync<int>(connection, "select count(*)::int from information_schema.tables where table_schema = @schema and table_type = 'BASE TABLE'", new NpgsqlParameter("schema", ExpectedSchema), ct);
            foreach (var table in RequiredTableNames)
            {
                required[table] = await ScalarAsync<bool>(connection, "select to_regclass(@name) is not null", new NpgsqlParameter("name", $"{ExpectedSchema}.{table}"), ct);
            }

            return new(true, schemaFound, tableCount, required, null, builder.Database ?? string.Empty, Mask(connectionString), version, []);
        }
        catch (Exception ex)
        {
            return new(false, false, 0, required, ex.Message, builder.Database ?? string.Empty, Mask(connectionString), version, []);
        }
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, NpgsqlParameter parameter, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(parameter);
        return (T)(await command.ExecuteScalarAsync(ct) ?? default(T)!);
    }

    private static string Mask(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password)) builder.Password = "******";
        return builder.ConnectionString;
    }
}
