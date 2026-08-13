using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddRecurringContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Sql);
    protected override void Down(MigrationBuilder migrationBuilder) { }

    private const string Sql = """
CREATE TABLE IF NOT EXISTS orcafacil.recurring_contracts (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, source_document_id uuid NULL, responsible_user_id uuid NULL,
 number varchar(40) NOT NULL, title varchar(180) NOT NULL, description varchar(2000), start_date date NOT NULL, end_date date,
 status varchar(32) NOT NULL DEFAULT 'Draft', recurring_amount numeric(18,2) NOT NULL, periodicity varchar(24) NOT NULL,
 custom_period_months integer, due_day integer NOT NULL, next_billing_date date, next_service_date date, commercial_terms varchar(4000),
 internal_notes varchar(4000), customer_notes varchar(4000), auto_renew boolean NOT NULL DEFAULT false, renewal_notice_days integer NOT NULL DEFAULT 30,
 response_sla_hours integer, execution_sla_hours integer, priority varchar(20) NOT NULL DEFAULT 'Normal', sla_uses_business_days boolean NOT NULL DEFAULT true,
 service_hours varchar(120), sla_notes varchar(1000), activated_at timestamptz, canceled_at timestamptz, cancellation_reason varchar(500),
 renewed_from_contract_id uuid, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
 CONSTRAINT ck_recurring_contract_due_day CHECK (due_day BETWEEN 1 AND 28), CONSTRAINT ck_recurring_contract_amount CHECK (recurring_amount > 0)
);
CREATE TABLE IF NOT EXISTS orcafacil.contract_items (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, service_catalog_item_id uuid, description varchar(500) NOT NULL,
 quantity numeric(18,4) NOT NULL, unit_price numeric(18,2) NOT NULL, checklist text, created_at timestamptz NOT NULL, updated_at timestamptz,
 is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT fk_contract_items_contract FOREIGN KEY(contract_id) REFERENCES orcafacil.recurring_contracts(id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS orcafacil.contract_payments (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, client_id uuid NOT NULL, competence date NOT NULL, due_date date NOT NULL,
 amount numeric(18,2) NOT NULL, payment_method varchar(40), status varchar(24) NOT NULL, paid_at timestamptz, notes varchar(1000), manual_payment_id uuid,
 created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
 CONSTRAINT fk_contract_payments_contract FOREIGN KEY(contract_id) REFERENCES orcafacil.recurring_contracts(id), CONSTRAINT ck_contract_payment_amount CHECK(amount > 0)
);
CREATE TABLE IF NOT EXISTS orcafacil.contract_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, user_id uuid NOT NULL, type varchar(50) NOT NULL, description varchar(1000) NOT NULL,
 related_entity_type varchar(50), related_entity_id uuid, related_url varchar(500), created_at timestamptz NOT NULL, updated_at timestamptz,
 is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT fk_contract_events_contract FOREIGN KEY(contract_id) REFERENCES orcafacil.recurring_contracts(id) ON DELETE CASCADE
);
ALTER TABLE orcafacil.work_orders ADD COLUMN IF NOT EXISTS contract_id uuid;
ALTER TABLE orcafacil.work_orders ADD COLUMN IF NOT EXISTS service_competence date;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_periodicity integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS suggested_monthly_price numeric(18,2);
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS estimated_monthly_cost numeric(18,2);
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_response_sla_hours integer;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_execution_sla_hours integer;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_checklist text;
CREATE UNIQUE INDEX IF NOT EXISTS ux_recurring_contract_account_number ON orcafacil.recurring_contracts(account_id,number);
CREATE UNIQUE INDEX IF NOT EXISTS ux_recurring_contract_source ON orcafacil.recurring_contracts(account_id,source_document_id) WHERE source_document_id IS NOT NULL AND is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_recurring_contract_client_status ON orcafacil.recurring_contracts(account_id,client_id,status);
CREATE INDEX IF NOT EXISTS ix_recurring_contract_end ON orcafacil.recurring_contracts(account_id,end_date);
CREATE INDEX IF NOT EXISTS ix_recurring_contract_next_billing ON orcafacil.recurring_contracts(account_id,next_billing_date);
CREATE INDEX IF NOT EXISTS ix_recurring_contract_next_service ON orcafacil.recurring_contracts(account_id,next_service_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contract_payment_competence ON orcafacil.contract_payments(account_id,contract_id,competence) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_contract_payment_due ON orcafacil.contract_payments(account_id,status,due_date);
CREATE INDEX IF NOT EXISTS ix_contract_event_timeline ON orcafacil.contract_events(account_id,contract_id,created_at);
CREATE UNIQUE INDEX IF NOT EXISTS ux_work_order_contract_competence ON orcafacil.work_orders(account_id,contract_id,service_competence) WHERE contract_id IS NOT NULL AND is_deleted=false;
""";
}
