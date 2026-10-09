using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

/// <summary>
/// Aloca números de documentos comerciais de forma estritamente atômica no PostgreSQL
/// por conta e tipo de documento, sem dependência de locks em memória de processo único.
/// </summary>
public sealed class AtomicDocumentNumberService(
    OrcaFacilDbContext db,
    ILogger<AtomicDocumentNumberService> logger) : IDocumentNumberService
{
    public async Task<string> NextAsync(Guid userId, DocumentType type, Guid? accountId = null, CancellationToken ct = default)
    {
        if (accountId is not Guid account || account == Guid.Empty)
            return await LegacyUserScopedNextAsync(userId, type, ct);

        var typeCode = type.ToString();
        var prefix = Prefix(type);
        var id = Guid.NewGuid();

        // Incremento atômico com RETURNING em uma única instrução SQL nativa no PostgreSQL.
        // O bloqueio de linha e a serialização de transações concorrentes são gerenciados
        // pelo próprio PostgreSQL via ON CONFLICT na restrição exclusiva (account_id, document_type).
        var next = await db.Database.SqlQuery<long>($"""
            INSERT INTO orcafacil.document_sequences
                (id, account_id, document_type, current_number, prefix, created_at, is_deleted)
            VALUES ({id}, {account}, {typeCode}, 1, {prefix}, now(), false)
            ON CONFLICT (account_id, document_type)
            DO UPDATE SET current_number = orcafacil.document_sequences.current_number + 1,
                          updated_at = now()
            RETURNING current_number AS "Value"
            """).SingleAsync(ct);

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
