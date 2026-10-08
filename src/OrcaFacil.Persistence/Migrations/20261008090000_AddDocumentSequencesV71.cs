using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>Adds tenant/type counters for commercial document numbers without renumbering existing documents.</summary>
public partial class AddDocumentSequencesV71 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS orcafacil.document_sequences (
          id uuid PRIMARY KEY,
          account_id uuid NOT NULL,
          document_type varchar(30) NOT NULL,
          current_number bigint NOT NULL DEFAULT 0,
          prefix varchar(12) NOT NULL,
          created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz,
          is_deleted boolean NOT NULL DEFAULT false
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sequences_account_type
          ON orcafacil.document_sequences(account_id, document_type);

        INSERT INTO orcafacil.document_sequences(id, account_id, document_type, current_number, prefix, created_at, is_deleted)
        SELECT gen_random_uuid(),
               account_id,
               type,
               COALESCE(MAX(CASE
                 WHEN number ~ ('^' || CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END || '-[0-9]+$')
                 THEN substring(number from length(CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END) + 2)::bigint
                 ELSE 0
               END), 0),
               CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END,
               now(),
               false
          FROM orcafacil.documents
         WHERE account_id IS NOT NULL AND number IS NOT NULL AND is_deleted = false
         GROUP BY account_id, type
        ON CONFLICT (account_id, document_type) DO UPDATE
          SET current_number = GREATEST(orcafacil.document_sequences.current_number, EXCLUDED.current_number),
              updated_at = now();

        CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number
          ON orcafacil.documents(account_id, type, number)
          WHERE account_id IS NOT NULL AND is_deleted = false;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Commercial numbers are historical evidence and are deliberately retained.
    }
}
