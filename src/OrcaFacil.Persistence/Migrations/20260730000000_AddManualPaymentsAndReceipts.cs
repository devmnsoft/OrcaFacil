using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260730000000_AddManualPaymentsAndReceipts")]
public sealed class AddManualPaymentsAndReceipts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE orcafacil.manual_payments (id uuid PRIMARY KEY, account_id uuid NOT NULL, work_order_id uuid NOT NULL REFERENCES orcafacil.work_orders(id) ON DELETE RESTRICT, document_id uuid, client_id uuid NOT NULL, amount numeric(18,2) NOT NULL CHECK (amount > 0), payment_method varchar(40) NOT NULL, paid_at timestamptz NOT NULL, notes varchar(1000), registered_by_user_id uuid NOT NULL, idempotency_key varchar(128) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX ux_manual_payments_idempotency ON orcafacil.manual_payments(account_id,idempotency_key);
CREATE INDEX ix_manual_payments_work_order ON orcafacil.manual_payments(account_id,work_order_id,paid_at);
CREATE TABLE orcafacil.receipts (id uuid PRIMARY KEY, account_id uuid NOT NULL, payment_id uuid NOT NULL REFERENCES orcafacil.manual_payments(id) ON DELETE RESTRICT, work_order_id uuid NOT NULL, client_id uuid NOT NULL, number varchar(40) NOT NULL, issuer_snapshot jsonb NOT NULL DEFAULT '{}', client_snapshot jsonb NOT NULL DEFAULT '{}', service_snapshot jsonb NOT NULL DEFAULT '[]', amount numeric(18,2) NOT NULL, amount_in_words varchar(500) NOT NULL, payment_method varchar(40) NOT NULL, issued_at timestamptz NOT NULL, city varchar(180), notes varchar(1000), fiscal_notice varchar(500) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX ux_receipts_number ON orcafacil.receipts(account_id,number);
CREATE UNIQUE INDEX ux_receipts_payment ON orcafacil.receipts(account_id,payment_id);
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
DROP TABLE IF EXISTS orcafacil.receipts;
DROP TABLE IF EXISTS orcafacil.manual_payments;
""");
}
