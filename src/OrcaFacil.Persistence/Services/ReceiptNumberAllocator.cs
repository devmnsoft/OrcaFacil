using Microsoft.EntityFrameworkCore;

namespace OrcaFacil.Persistence.Services;

/// <summary>Allocates the tenant receipt sequence already used by the receipt tables.</summary>
internal static class ReceiptNumberAllocator
{
    public static async Task<string> NextAsync(OrcaFacilDbContext db, Guid accountId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO orcafacil.receipt_sequences
                (id, account_id, year, current_number, prefix, created_at, is_deleted)
            VALUES ({Guid.NewGuid()}, {accountId}, {year}, 0, {"REC"}, now(), false)
            ON CONFLICT (account_id, year) DO NOTHING
            """, ct);
        var sequence = await db.ReceiptSequences.FromSqlInterpolated($"""
            SELECT * FROM orcafacil.receipt_sequences
             WHERE account_id = {accountId} AND year = {year}
             FOR UPDATE
            """).SingleAsync(ct);
        sequence.CurrentNumber++;
        sequence.Touch();
        await db.SaveChangesAsync(ct);
        return $"{sequence.Prefix}-{year}-{sequence.CurrentNumber:000000}";
    }
}
