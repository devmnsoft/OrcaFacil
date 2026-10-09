using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace OrcaFacil.Persistence.Services;

/// <summary>Allocates the tenant receipt sequence atomically in PostgreSQL.</summary>
internal static class ReceiptNumberAllocator
{
    private static long _inMemoryCounter;

    public static async Task<string> NextAsync(OrcaFacilDbContext db, Guid accountId, CancellationToken ct)
    {
        if (accountId == Guid.Empty)
            throw new InvalidOperationException("Conta comercial obrigatória para alocação de sequência de recibo.");

        var year = DateTime.UtcNow.Year;
        const string prefix = "REC";

        if (!db.Database.IsRelational())
        {
            var simulated = Interlocked.Increment(ref _inMemoryCounter);
            return $"{prefix}-{year}-{simulated:000000}";
        }

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = """
            INSERT INTO orcafacil.receipt_sequences
                (id, account_id, year, current_number, prefix, created_at, is_deleted)
            VALUES (@id, @accountId, @year, 1, @prefix, now(), false)
            ON CONFLICT (account_id, year)
            DO UPDATE SET current_number = orcafacil.receipt_sequences.current_number + 1,
                          updated_at = now()
            RETURNING current_number;
            """;

        var pId = cmd.CreateParameter();
        pId.ParameterName = "@id";
        pId.Value = Guid.NewGuid();
        cmd.Parameters.Add(pId);

        var pAcc = cmd.CreateParameter();
        pAcc.ParameterName = "@accountId";
        pAcc.Value = accountId;
        cmd.Parameters.Add(pAcc);

        var pYear = cmd.CreateParameter();
        pYear.ParameterName = "@year";
        pYear.Value = year;
        cmd.Parameters.Add(pYear);

        var pPrefix = cmd.CreateParameter();
        pPrefix.ParameterName = "@prefix";
        pPrefix.Value = prefix;
        cmd.Parameters.Add(pPrefix);

        var result = await cmd.ExecuteScalarAsync(ct)
            ?? throw new InvalidOperationException("Falha ao alocar número sequencial de recibo no PostgreSQL.");

        var next = Convert.ToInt64(result);
        return $"{prefix}-{year}-{next:000000}";
    }
}
