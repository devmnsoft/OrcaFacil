namespace OrcaFacil.Application.Abstractions;

public interface IDatabaseDiagnosticsService
{
    Task<DatabaseDiagnosticsDto> CheckAsync(CancellationToken ct = default);
    Task<bool> CanConnectForUserActionAsync(CancellationToken ct = default);
}

public sealed record DatabaseDiagnosticsDto(
    bool CanConnect,
    bool SchemaExists,
    IReadOnlyList<string> ExistingTables,
    IReadOnlyList<string> MissingTables,
    string? DatabaseName,
    string? PostgreSqlVersion,
    string? Error,
    bool FreePlanExists = false,
    bool PublishedFreeVersionExists = false,
    IReadOnlyList<string>? MissingColumns = null,
    IReadOnlyList<string>? MissingIndexes = null,
    string? ConnectedUser = null,
    string? SearchPath = null,
    long LatencyMilliseconds = 0,
    bool CanRead = false,
    bool CanWrite = false,
    IReadOnlyList<string>? AppliedMigrations = null);
