using System.Text.Json;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence;

public class LoggerService : ILoggerService
{
    private readonly OrcaFacilDbContext _db;

    public LoggerService(OrcaFacilDbContext db) => _db = db;

    public Task RegisterAsync(Guid? userId, string eventName, object? metadata, CancellationToken ct = default)
    {
        _db.SystemLogs.Add(new SystemLog
        {
            UserId = userId,
            Type = eventName,
            Message = eventName,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
        });
        return Task.CompletedTask;
    }
}
