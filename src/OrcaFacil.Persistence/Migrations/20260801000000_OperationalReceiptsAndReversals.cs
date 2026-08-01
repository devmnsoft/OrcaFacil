using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260801000000_OperationalReceiptsAndReversals")]
public sealed class OperationalReceiptsAndReversals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE orcafacil.manual_payments ALTER COLUMN work_order_id DROP NOT NULL;
ALTER TABLE orcafacil.manual_payments
  ADD COLUMN IF NOT EXISTS status varchar(24) NOT NULL DEFAULT 'Active',
  ADD COLUMN IF NOT EXISTS reversed_at timestamptz,
  ADD COLUMN IF NOT EXISTS reversed_by_user_id uuid,
  ADD COLUMN IF NOT EXISTS reversal_reason varchar(500);
ALTER TABLE orcafacil.receipts ALTER COLUMN work_order_id DROP NOT NULL;
ALTER TABLE orcafacil.receipts
  ADD COLUMN IF NOT EXISTS document_id uuid,
  ADD COLUMN IF NOT EXISTS legacy_document_id uuid,
  ADD COLUMN IF NOT EXISTS origin_type varchar(24) NOT NULL DEFAULT 'WorkOrder',
  ADD COLUMN IF NOT EXISTS service_description varchar(1000) NOT NULL DEFAULT '',
  ADD COLUMN IF NOT EXISTS cancelled_at timestamptz,
  ADD COLUMN IF NOT EXISTS cancelled_by_user_id uuid,
  ADD COLUMN IF NOT EXISTS cancellation_reason varchar(500),
  ADD COLUMN IF NOT EXISTS pdf_storage_key varchar(500),
  ADD COLUMN IF NOT EXISTS sent_at timestamptz,
  ADD COLUMN IF NOT EXISTS last_shared_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_manual_payments_account_id_status_paid_at ON orcafacil.manual_payments(account_id,status,paid_at);
CREATE INDEX IF NOT EXISTS ix_receipts_account_id_origin_type_issued_at ON orcafacil.receipts(account_id,origin_type,issued_at);
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
DROP INDEX IF EXISTS orcafacil.ix_receipts_account_id_origin_type_issued_at;
DROP INDEX IF EXISTS orcafacil.ix_manual_payments_account_id_status_paid_at;
ALTER TABLE orcafacil.receipts DROP COLUMN IF EXISTS last_shared_at, DROP COLUMN IF EXISTS sent_at,
 DROP COLUMN IF EXISTS pdf_storage_key, DROP COLUMN IF EXISTS cancellation_reason, DROP COLUMN IF EXISTS cancelled_by_user_id,
 DROP COLUMN IF EXISTS cancelled_at, DROP COLUMN IF EXISTS service_description, DROP COLUMN IF EXISTS origin_type,
 DROP COLUMN IF EXISTS legacy_document_id, DROP COLUMN IF EXISTS document_id;
ALTER TABLE orcafacil.manual_payments DROP COLUMN IF EXISTS reversal_reason, DROP COLUMN IF EXISTS reversed_by_user_id,
 DROP COLUMN IF EXISTS reversed_at, DROP COLUMN IF EXISTS status;
""");
}
