using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>Adds a tenant/year counter without changing or renumbering existing receipts.</summary>
public partial class AddReceiptSequences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS orcafacil.receipt_sequences (
          id uuid PRIMARY KEY, account_id uuid NOT NULL, year integer NOT NULL,
          current_number bigint NOT NULL DEFAULT 0, prefix varchar(12) NOT NULL DEFAULT 'REC',
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
          is_deleted boolean NOT NULL DEFAULT false
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ux_receipt_sequences_account_year
          ON orcafacil.receipt_sequences(account_id, year);
        INSERT INTO orcafacil.receipt_sequences(id, account_id, year, current_number, prefix, created_at, is_deleted)
        SELECT gen_random_uuid(), account_id, EXTRACT(YEAR FROM issued_at)::integer, COUNT(*), 'REC', now(), false
          FROM orcafacil.receipts WHERE is_deleted = false
         GROUP BY account_id, EXTRACT(YEAR FROM issued_at)::integer
        ON CONFLICT (account_id, year) DO UPDATE
          SET current_number = GREATEST(orcafacil.receipt_sequences.current_number, EXCLUDED.current_number), updated_at = now();
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Receipt numbering history is deliberately retained.
    }
}
