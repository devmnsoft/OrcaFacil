using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Plans;

public class UserUsageService
{
    private readonly IRepository<UserUsage> _usage;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UserUsageService> _logger;

    public UserUsageService(IRepository<UserUsage> usage, IUnitOfWork uow, ILogger<UserUsageService> logger)
    {
        _usage = usage;
        _uow = uow;
        _logger = logger;
    }

    public Task<UserUsage?> GetCurrentAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_usage.Query().SingleOrDefault(item => item.UserId == userId && item.Period == Period()));

    public async Task RegisterDocumentAsync(Guid userId, DocumentType type, CancellationToken ct = default)
    {
        var item = await GetOrCreateAsync(userId, null, ct);
        item.DocumentsCreated++;
        if (type == DocumentType.Budget) item.BudgetsCreated++;
        if (type == DocumentType.Receipt) item.ReceiptsCreated++;
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("USER_USAGE_DOCUMENT_INCREMENTED {UserId} {Type}", userId, type);
    }

    public async Task RegisterPdfAsync(Guid userId, Guid? accountId, CancellationToken ct = default)
    {
        var item = await GetOrCreateAsync(userId, accountId, ct);
        if (accountId.HasValue && item.AccountId is null) item.AccountId = accountId.Value;
        item.PdfGenerated++;
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("USER_USAGE_PDF_INCREMENTED {UserId} {AccountId}", userId, accountId);
    }

    public async Task RegisterBackupExportAsync(Guid userId, Guid? accountId, CancellationToken ct = default)
    {
        var item = await GetOrCreateAsync(userId, accountId, ct);
        if (accountId.HasValue && item.AccountId is null) item.AccountId = accountId.Value;
        item.BackupExports++;
        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("USER_USAGE_BACKUP_EXPORT_INCREMENTED {UserId} {AccountId}", userId, accountId);
    }

    private async Task<UserUsage> GetOrCreateAsync(Guid userId, Guid? accountId, CancellationToken ct)
    {
        var item = await GetCurrentAsync(userId, ct);
        if (item is not null) return item;
        item = new UserUsage { UserId = userId, AccountId = accountId, Period = Period() };
        await _usage.AddAsync(item, ct);
        return item;
    }

    private static string Period() => DateTime.UtcNow.ToString("yyyy-MM");
}
