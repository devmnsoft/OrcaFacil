namespace OrcaFacil.Application.Abstractions;

public interface IDatabaseDiagnosticsService
{
    Task<DatabaseDiagnosticsDto> CheckAsync(CancellationToken ct = default);
}

public sealed record DatabaseDiagnosticsDto(
    bool CanConnect,
    bool SchemaExists,
    IReadOnlyList<string> ExistingTables,
    IReadOnlyList<string> MissingTables,
    string? DatabaseName,
    string? PostgreSqlVersion,
    string? Error);
