using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260731000000_AddOperationalBudgetDraft")]
public sealed class AddOperationalBudgetDraft : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE orcafacil.documents
  ADD COLUMN IF NOT EXISTS client_snapshot jsonb,
  ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS expected_start_at timestamptz,
  ADD COLUMN IF NOT EXISTS estimated_duration varchar(120),
  ADD COLUMN IF NOT EXISTS payment_method varchar(60),
  ADD COLUMN IF NOT EXISTS installment_count integer,
  ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2),
  ADD COLUMN IF NOT EXISTS pix_information varchar(300),
  ADD COLUMN IF NOT EXISTS warranty_text varchar(2000),
  ADD COLUMN IF NOT EXISTS conditions_text varchar(4000),
  ADD COLUMN IF NOT EXISTS template_code varchar(40) NOT NULL DEFAULT 'essential',
  ADD COLUMN IF NOT EXISTS template_snapshot jsonb,
  ADD COLUMN IF NOT EXISTS row_version bytea NOT NULL DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex'),
  ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz,
  ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.document_items
  ADD COLUMN IF NOT EXISTS service_catalog_item_id uuid,
  ADD COLUMN IF NOT EXISTS unit varchar(40) NOT NULL DEFAULT 'serviço',
  ADD COLUMN IF NOT EXISTS notes varchar(1000),
  ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;
CREATE INDEX IF NOT EXISTS ix_documents_account_draft_autosave ON orcafacil.documents(account_id, status, last_autosaved_at);
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
DROP INDEX IF EXISTS orcafacil.ix_documents_account_draft_autosave;
ALTER TABLE orcafacil.document_items DROP COLUMN IF EXISTS service_catalog_item_id, DROP COLUMN IF EXISTS unit, DROP COLUMN IF EXISTS notes, DROP COLUMN IF EXISTS sort_order;
ALTER TABLE orcafacil.documents DROP COLUMN IF EXISTS client_snapshot, DROP COLUMN IF EXISTS current_wizard_step, DROP COLUMN IF EXISTS expected_start_at, DROP COLUMN IF EXISTS estimated_duration, DROP COLUMN IF EXISTS payment_method, DROP COLUMN IF EXISTS installment_count, DROP COLUMN IF EXISTS deposit_amount, DROP COLUMN IF EXISTS pix_information, DROP COLUMN IF EXISTS warranty_text, DROP COLUMN IF EXISTS conditions_text, DROP COLUMN IF EXISTS template_code, DROP COLUMN IF EXISTS template_snapshot, DROP COLUMN IF EXISTS row_version, DROP COLUMN IF EXISTS last_autosaved_at, DROP COLUMN IF EXISTS last_autosave_key;
""");
}
