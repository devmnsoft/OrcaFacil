namespace OrcaFacil.Application.Abstractions;

public interface IDatabaseSchemaContractService
{
    Task<DatabaseSchemaContractResult> CheckRegistrationContractAsync(CancellationToken ct = default);
}

public sealed record DatabaseSchemaContractIssue(
    string Table,
    string? Column,
    string State,
    string RecommendedMigration);

public sealed record DatabaseSchemaContractResult(
    bool IsValid,
    IReadOnlyList<DatabaseSchemaContractIssue> Issues,
    DateTimeOffset CheckedAt,
    bool HasPendingMigrations);
