namespace OrcaFacil.Application.Abstractions;

public interface IAuditService
{
    Task RegisterAsync(Guid? userId, string action, string entityType, string? entityId, object? before,
        object? after, object? metadata, CancellationToken ct = default, Guid? accountId = null);
}
