using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

/// <summary>Allocates commercial document numbers with a PostgreSQL row lock per account and document type.</summary>
public sealed class AtomicDocumentNumberService(
    OrcaFacilDbContext db,
    ILogger<AtomicDocumentNumberService> logger) : IDocumentNumberService
{
    public async Task<string> NextAsync(Guid userId, DocumentType type, Guid? accountId = null, CancellationToken ct = default)
    {
        if (accountId is not Guid account)
            return await LegacyUserScopedNextAsync(userId, type, ct);

        var typeCode = type.ToString();
        var prefix = Prefix(type);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO orcafacil.document_sequences
                (id, account_id, document_type, current_number, prefix, created_at, is_deleted)
            VALUES ({Guid.NewGuid()}, {account}, {typeCode}, 0, {prefix}, now(), false)
            ON CONFLICT (account_id, document_type) DO NOTHING
            """, ct);

        var current = await db.Database.SqlQuery<long>($"""
            SELECT current_number AS "Value"
              FROM orcafacil.document_sequences
             WHERE account_id = {account} AND document_type = {typeCode}
             FOR UPDATE
            """).SingleAsync(ct);
        var next = current + 1;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE orcafacil.document_sequences
               SET current_number = {next}, updated_at = now()
             WHERE account_id = {account} AND document_type = {typeCode}
            """, ct);

        var number = $"{prefix}-{next:000000}";
        logger.LogInformation("DOCUMENT_NUMBER_GENERATED {AccountId} {UserId} {DocumentType} {Number}",
            account, userId, type, number);
        return number;
    }

    private async Task<string> LegacyUserScopedNextAsync(Guid userId, DocumentType type, CancellationToken ct)
    {
        var prefix = Prefix(type);
        var existing = await db.Documents.AsNoTracking()
            .Where(document => document.UserId == userId && document.Type == type)
            .Select(document => document.Number)
            .ToListAsync(ct);
        var sequence = existing
            .Select(number => long.TryParse(number.Replace(prefix + "-", string.Empty), out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var next = $"{prefix}-{sequence:000000}";
        logger.LogWarning("DOCUMENT_NUMBER_GENERATED_WITH_LEGACY_USER_SCOPE {UserId} {DocumentType} {Number}",
            userId, type, next);
        return next;
    }

    private static string Prefix(DocumentType type) => type == DocumentType.Receipt ? "REC" : "ORC";
}
