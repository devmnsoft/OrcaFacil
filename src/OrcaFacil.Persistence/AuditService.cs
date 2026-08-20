using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence;

public class AuditService : IAuditService
{
    private readonly OrcaFacilDbContext _db;
    private readonly ISensitiveDataSanitizer _sanitizer;

    public AuditService(OrcaFacilDbContext db, ISensitiveDataSanitizer sanitizer)
    {
        _db = db;
        _sanitizer = sanitizer;
    }

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
            Summary = $"{action} em {entityType}",
            CorrelationId = Guid.NewGuid(),
            BeforeJson = before is null ? null : _sanitizer.SanitizeJson(before),
            AfterJson = after is null ? null : _sanitizer.SanitizeJson(after),
            MetadataJson = metadata is null ? null : _sanitizer.SanitizeJson(metadata),
        });
        return Task.CompletedTask;
    }
}
