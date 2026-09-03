using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>Restores the complete Document EF contract in place and preserves every business row.</summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260903020000_FixDocumentsTemplateCodeFullSchemaDriftV61")]
public sealed class FixDocumentsTemplateCodeFullSchemaDriftV61 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_code varchar(40);
        UPDATE orcafacil.documents SET template_code = 'essential' WHERE template_code IS NULL OR btrim(template_code) = '';
        ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET DEFAULT 'essential';
        ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET NOT NULL;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
        UPDATE orcafacil.documents SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex') WHERE row_version IS NULL;
        ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');
        ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
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
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_team_id uuid;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_to_user_id uuid;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS business_unit_id uuid;
        ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_city varchar(120);
        CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
        CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
        CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
        CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
        CREATE INDEX IF NOT EXISTS ix_documents_template_code ON orcafacil.documents(account_id, template_code) WHERE template_code IS NOT NULL AND is_deleted = false;
        """);

    // Destructive rollback is deliberately disabled: this migration may repair restored production data.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
