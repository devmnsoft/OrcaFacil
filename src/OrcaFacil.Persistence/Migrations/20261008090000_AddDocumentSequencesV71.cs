using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>Adds tenant/type counters for commercial document numbers without renumbering existing documents.</summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261008090000_AddDocumentSequencesV71")]
public sealed class AddDocumentSequencesV71 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(AddDocumentSequencesV71).Assembly;
        var name = assembly.GetManifestResourceNames()
            .SingleOrDefault(x => x.EndsWith("hotfix_document_sequences_v71.sql", StringComparison.Ordinal));

        if (name is not null)
        {
            using var stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException("Script SQL da V7.1 não encontrado no stream.");
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }
        else
        {
            // Fallback inline seguro com a mesma lógica do hotfix
            migrationBuilder.Sql("""
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

                UPDATE orcafacil.documents d
                   SET account_id = m.account_id,
                       updated_at = now()
                  FROM (
                    SELECT user_id, min(account_id) AS account_id
                      FROM orcafacil.account_members
                     WHERE is_deleted = false
                     GROUP BY user_id
                    HAVING count(DISTINCT account_id) = 1
                  ) m
                 WHERE d.account_id IS NULL
                   AND d.user_id = m.user_id;

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
                 WHERE account_id IS NOT NULL AND number IS NOT NULL
                 GROUP BY account_id, type
                ON CONFLICT (account_id, document_type) DO UPDATE
                  SET current_number = GREATEST(orcafacil.document_sequences.current_number, EXCLUDED.current_number),
                      updated_at = now();

                CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number
                  ON orcafacil.documents(account_id, type, number)
                  WHERE account_id IS NOT NULL AND is_deleted = false;
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Números comerciais e contadores históricos são registros de auditoria e deliberadamente preservados.
    }
}
