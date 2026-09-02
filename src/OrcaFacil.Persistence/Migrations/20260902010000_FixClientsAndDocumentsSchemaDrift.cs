using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>Non-destructively repairs client and commercial-document schema drift in restored databases.</summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260902010000_FixClientsAndDocumentsSchemaDrift")]
public sealed class FixClientsAndDocumentsSchemaDrift : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS deleted_by uuid;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);

CREATE INDEX IF NOT EXISTS ix_clients_account_active ON orcafacil.clients(account_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_clients_account_name ON orcafacil.clients(account_id, name) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
""");

    // A rollback must never discard restored customer or commercial data.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
