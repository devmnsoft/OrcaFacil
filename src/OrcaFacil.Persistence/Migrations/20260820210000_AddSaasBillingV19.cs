using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddSaasBillingV19 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Sql);
    protected override void Down(MigrationBuilder migrationBuilder) { /* Billing history is intentionally retained. */ }

    private const string Sql = """
ALTER TABLE orcafacil.billing_invoices ADD COLUMN IF NOT EXISTS invoice_number varchar(60) NOT NULL DEFAULT '';
ALTER TABLE orcafacil.billing_invoices ADD COLUMN IF NOT EXISTS issue_date timestamptz NOT NULL DEFAULT now();
ALTER TABLE orcafacil.billing_invoices ADD COLUMN IF NOT EXISTS subtotal numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.billing_invoices ADD COLUMN IF NOT EXISTS discount_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.billing_invoices ADD COLUMN IF NOT EXISTS paid_amount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.billing_invoices ADD COLUMN IF NOT EXISTS notes text;
ALTER TABLE orcafacil.billing_invoices ADD COLUMN IF NOT EXISTS cancellation_reason varchar(500);
CREATE TABLE IF NOT EXISTS orcafacil.billing_invoice_items (id uuid PRIMARY KEY, invoice_id uuid NOT NULL, description varchar(240) NOT NULL, quantity numeric(14,3) NOT NULL, unit_amount numeric(18,2) NOT NULL, total_amount numeric(18,2) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_billing_invoice_items_invoice_id ON orcafacil.billing_invoice_items(invoice_id);
CREATE TABLE IF NOT EXISTS orcafacil.billing_payments (id uuid PRIMARY KEY, account_id uuid NOT NULL, invoice_id uuid NOT NULL, amount numeric(18,2) NOT NULL CHECK (amount > 0), payment_date timestamptz NOT NULL, payment_method varchar(40) NOT NULL, reference varchar(160), status varchar(30) NOT NULL, registered_by_user_id uuid NOT NULL, reversal_reason varchar(500), created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_billing_payments_account_invoice ON orcafacil.billing_payments(account_id, invoice_id);
CREATE TABLE IF NOT EXISTS orcafacil.subscription_change_requests (id uuid PRIMARY KEY, account_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, current_plan_id uuid NULL, requested_plan_id uuid NULL, request_type varchar(40) NOT NULL, status varchar(30) NOT NULL, reason varchar(1000) NOT NULL, admin_notes varchar(1000), reviewed_at timestamptz, completed_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_subscription_change_requests_account_status ON orcafacil.subscription_change_requests(account_id,status);
CREATE TABLE IF NOT EXISTS orcafacil.plan_addons (id uuid PRIMARY KEY, code varchar(80) NOT NULL UNIQUE, name text NOT NULL, description text NOT NULL, price_monthly numeric(18,2) NOT NULL, price_annual numeric(18,2) NOT NULL, limit_type text NOT NULL, limit_increment bigint NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.account_addons (id uuid PRIMARY KEY, account_id uuid NOT NULL, addon_id uuid NOT NULL, quantity integer NOT NULL CHECK(quantity > 0), activated_at timestamptz NOT NULL, deactivated_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_account_addons_account ON orcafacil.account_addons(account_id,addon_id,deactivated_at);
CREATE TABLE IF NOT EXISTS orcafacil.account_entitlements (id uuid PRIMARY KEY, account_id uuid NOT NULL, feature_code varchar(100) NOT NULL, is_enabled boolean NOT NULL, limit_value bigint, source text NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_entitlements_account_feature ON orcafacil.account_entitlements(account_id,feature_code);
""";
}
