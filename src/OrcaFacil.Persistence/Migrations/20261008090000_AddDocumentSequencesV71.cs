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
            // Fallback inline idêntico e seguro com a mesma lógica do hotfix
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
                   SET account_id = eligible.account_id,
                       updated_at = now()
                  FROM (
                    SELECT m.user_id, (array_agg(m.account_id))[1] AS account_id
                      FROM orcafacil.account_members m
                      INNER JOIN orcafacil.business_accounts a ON a.id = m.account_id
                     WHERE m.is_deleted = false
                       AND m.status = 1
                       AND a.is_deleted = false
                       AND a.status = 1
                     GROUP BY m.user_id
                    HAVING count(DISTINCT m.account_id) = 1
                  ) eligible
                 WHERE d.account_id IS NULL
                   AND d.user_id = eligible.user_id;

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

                DO $$
                DECLARE
                  dup_record RECORD;
                BEGIN
                  FOR dup_record IN
                    SELECT account_id, type, number, array_agg(id ORDER BY created_at ASC, id ASC) AS doc_ids
                      FROM orcafacil.documents
                     WHERE account_id IS NOT NULL AND is_deleted = false AND number IS NOT NULL
                     GROUP BY account_id, type, number
                    HAVING count(*) > 1
                  LOOP
                    FOR i IN 2..array_length(dup_record.doc_ids, 1) LOOP
                      UPDATE orcafacil.documents
                         SET number = number || '-HIST-' || substring(dup_record.doc_ids[i]::text, 1, 8),
                             updated_at = now()
                       WHERE id = dup_record.doc_ids[i];
                    END LOOP;
                  END LOOP;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number
                  ON orcafacil.documents(account_id, type, number)
                  WHERE account_id IS NOT NULL AND is_deleted = false;

                CREATE OR REPLACE FUNCTION orcafacil.fn_bump_client_version()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.version := (COALESCE(OLD.version::bigint, 0) + 1)::text::xid;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_bump_client_version ON orcafacil.clients;
                CREATE TRIGGER trg_bump_client_version
                BEFORE UPDATE ON orcafacil.clients
                FOR EACH ROW
                EXECUTE FUNCTION orcafacil.fn_bump_client_version();

                DO $$
                BEGIN
                  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts') THEN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts' AND column_name = 'whats_app') THEN
                      ALTER TABLE orcafacil.partner_contacts ADD COLUMN whats_app varchar(40);
                      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts' AND column_name = 'whatsapp') THEN
                        UPDATE orcafacil.partner_contacts SET whats_app = whatsapp WHERE whats_app IS NULL;
                      END IF;
                    END IF;
                  END IF;

                  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles') THEN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles' AND column_name = 'whats_app') THEN
                      ALTER TABLE orcafacil.partner_profiles ADD COLUMN whats_app varchar(40);
                      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles' AND column_name = 'whatsapp') THEN
                        UPDATE orcafacil.partner_profiles SET whats_app = whatsapp WHERE whats_app IS NULL;
                      END IF;
                    END IF;
                  END IF;

                  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'suppliers') THEN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'suppliers' AND column_name = 'whats_app') THEN
                      ALTER TABLE orcafacil.suppliers ADD COLUMN whats_app varchar(40);
                      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'suppliers' AND column_name = 'whatsapp') THEN
                        UPDATE orcafacil.suppliers SET whats_app = whatsapp WHERE whats_app IS NULL;
                      END IF;
                    END IF;
                  END IF;
                END $$;
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Números comerciais e contadores históricos são registros de auditoria e deliberadamente preservados.
    }
}
