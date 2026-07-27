using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>
/// Additive transition to account isolation. Legacy user columns remain available for authorship
/// and rollback compatibility; no business data is removed.
/// </summary>
public partial class ConsolidateAccountIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS account_id uuid;
            ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_id uuid;
            ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS account_id uuid;
            ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS account_id uuid;
            ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS billing_invoice_id uuid;
            ALTER TABLE orcafacil.user_usage ADD COLUMN IF NOT EXISTS account_id uuid;
            ALTER TABLE orcafacil.public_quotes ADD COLUMN IF NOT EXISTS account_id uuid;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;

            UPDATE orcafacil.documents d SET account_id = m.account_id
              FROM orcafacil.account_members m
             WHERE d.account_id IS NULL AND d.user_id = m.user_id AND m.role_code = 'Owner' AND NOT m.is_deleted;
            UPDATE orcafacil.clients c SET account_id = m.account_id
              FROM orcafacil.account_members m
             WHERE c.account_id IS NULL AND c.user_id = m.user_id AND m.role_code = 'Owner' AND NOT m.is_deleted;
            UPDATE orcafacil.payments p SET account_id = m.account_id
              FROM orcafacil.account_members m
             WHERE p.account_id IS NULL AND p.user_id = m.user_id AND m.role_code = 'Owner' AND NOT m.is_deleted;
            UPDATE orcafacil.user_usage u SET account_id = m.account_id
              FROM orcafacil.account_members m
             WHERE u.account_id IS NULL AND u.user_id = m.user_id AND m.role_code = 'Owner' AND NOT m.is_deleted;
            UPDATE orcafacil.public_quotes q SET account_id = d.account_id
              FROM orcafacil.documents d WHERE q.account_id IS NULL AND q.document_id = d.id;

            CREATE INDEX IF NOT EXISTS ix_documents_account_id_created_at ON orcafacil.documents(account_id, created_at);
            CREATE INDEX IF NOT EXISTS ix_documents_account_id_client_id ON orcafacil.documents(account_id, client_id);
            CREATE INDEX IF NOT EXISTS ix_clients_account_id ON orcafacil.clients(account_id);
            CREATE INDEX IF NOT EXISTS ix_payments_account_id ON orcafacil.payments(account_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_payments_idempotency_key ON orcafacil.payments(idempotency_key) WHERE idempotency_key IS NOT NULL;
            CREATE INDEX IF NOT EXISTS ix_user_usage_account_id_period ON orcafacil.user_usage(account_id, period);
            CREATE INDEX IF NOT EXISTS ix_public_quotes_account_id_created_at ON orcafacil.public_quotes(account_id, created_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty: the consolidation is additive and production-safe.
    }
}
