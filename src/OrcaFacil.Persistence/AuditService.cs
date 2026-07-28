using System.Text.Json;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence;

public class AuditService : IAuditService
{
    private readonly OrcaFacilDbContext _db;

    public AuditService(OrcaFacilDbContext db) => _db = db;

    public Task RegisterAsync(Guid? userId, string action, string entityType, string? entityId, object? before,
        object? after, object? metadata, CancellationToken ct = default, Guid? accountId = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            AccountId = accountId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
        });
        return Task.CompletedTask;
    }
}
