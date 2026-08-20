using System.Text;
using System.Text.Json;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Privacy;

public sealed record DataExportResult(string FileName, string ContentType, byte[] Content, Guid JobId);

public sealed class DataExportService(IRepository<DataExportJob> jobs, IUnitOfWork unitOfWork,
    ISensitiveDataSanitizer sanitizer, IAuditService audit)
{
    public async Task<DataExportResult> ExportJsonAsync(Guid accountId, Guid userId, string scope, object accountScopedData,
        bool canExportFinancial, bool includesFinancial, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty || userId == Guid.Empty) throw new UnauthorizedAccessException();
        if (includesFinancial && !canExportFinancial) throw new UnauthorizedAccessException("Permissão financeira necessária.");
        var job = new DataExportJob { AccountId = accountId, RequestedByUserId = userId, Scope = scope,
            RequestedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(24) };
        await jobs.AddAsync(job, ct);
        var envelope = new { ExportedAt = DateTime.UtcNow, AccountId = accountId, Scope = scope,
            Data = JsonDocument.Parse(sanitizer.SanitizeJson(accountScopedData)).RootElement };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true }));
        await audit.RegisterAsync(userId, "Privacy.DataExported", nameof(DataExportJob), job.Id.ToString(), null,
            new { job.Scope, job.Format }, new { IncludesFinancial = includesFinancial }, ct, accountId);
        await unitOfWork.SaveChangesAsync(ct);
        return new($"orcafacil-export-{job.Id:N}.json", "application/json", bytes, job.Id);
    }
}
