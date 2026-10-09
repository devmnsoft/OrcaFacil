using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private static long _inMemoryCounter;

    public async Task<string> NextAsync(Guid userId, DocumentType type, Guid? accountId = null, CancellationToken ct = default)
    {
        if (accountId is not Guid account || account == Guid.Empty)
            throw new InvalidOperationException("Conta comercial obrigatória para alocação de sequência de documento.");

        var typeCode = type.ToString();
        var prefix = Prefix(type);
        var id = Guid.NewGuid();

        long next;
        if (!db.Database.IsRelational())
        {
            next = Interlocked.Increment(ref _inMemoryCounter);
        }
        else
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            await using var cmd = connection.CreateCommand();
            cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            cmd.CommandText = """
                INSERT INTO orcafacil.document_sequences
                    (id, account_id, document_type, current_number, prefix, created_at, is_deleted)
                VALUES (@id, @accountId, @documentType, 1, @prefix, now(), false)
                ON CONFLICT (account_id, document_type)
                DO UPDATE SET current_number = orcafacil.document_sequences.current_number + 1,
                              updated_at = now()
                RETURNING current_number;
                """;

            var pId = cmd.CreateParameter();
            pId.ParameterName = "@id";
            pId.Value = id;
            cmd.Parameters.Add(pId);

            var pAcc = cmd.CreateParameter();
            pAcc.ParameterName = "@accountId";
            pAcc.Value = account;
            cmd.Parameters.Add(pAcc);

            var pType = cmd.CreateParameter();
            pType.ParameterName = "@documentType";
            pType.Value = typeCode;
            cmd.Parameters.Add(pType);

            var pPrefix = cmd.CreateParameter();
            pPrefix.ParameterName = "@prefix";
            pPrefix.Value = prefix;
            cmd.Parameters.Add(pPrefix);

            var scalarResult = await cmd.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Falha ao alocar sequência atômica no banco de dados.");
            next = Convert.ToInt64(scalarResult);
        }

        var number = $"{prefix}-{next:000000}";
        logger.LogInformation("DOCUMENT_NUMBER_GENERATED {AccountId} {UserId} {DocumentType} {Number}",
            account, userId, type, number);
        return number;
    }

    private static string Prefix(DocumentType type) => type == DocumentType.Receipt ? "REC" : "ORC";
}
