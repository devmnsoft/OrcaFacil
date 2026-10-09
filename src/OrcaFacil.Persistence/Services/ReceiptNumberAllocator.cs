using Microsoft.EntityFrameworkCore;

namespace OrcaFacil.Persistence.Services;

/// <summary>Allocates the tenant receipt sequence atomically in PostgreSQL.</summary>
internal static class ReceiptNumberAllocator
{
    public static async Task<string> NextAsync(OrcaFacilDbContext db, Guid accountId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var id = Guid.NewGuid();
        var next = await db.Database.SqlQuery<long>($"""
            INSERT INTO orcafacil.receipt_sequences
                (id, account_id, year, current_number, prefix, created_at, is_deleted)
            VALUES ({id}, {accountId}, {year}, 1, {"REC"}, now(), false)
            ON CONFLICT (account_id, year)
            DO UPDATE SET current_number = orcafacil.receipt_sequences.current_number + 1,
                          updated_at = now()
            RETURNING current_number AS "Value"
            """).SingleAsync(ct);

        return $"REC-{year}-{next:000000}";
    }
}
