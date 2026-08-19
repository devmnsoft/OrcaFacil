using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddFinancialEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS orcafacil.financial_entries (
              id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL,
              document_id uuid, work_order_id uuid, contract_id uuid, contract_payment_id uuid,
              origin varchar(24) NOT NULL, description varchar(500) NOT NULL, due_date date NOT NULL,
              amount numeric(18,2) NOT NULL, paid_amount numeric(18,2) NOT NULL DEFAULT 0,
              status varchar(24) NOT NULL DEFAULT 'Pending', canceled_at timestamptz,
              canceled_by_user_id uuid, cancellation_reason varchar(500), created_at timestamptz NOT NULL,
              updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
              CONSTRAINT ck_financial_entry_amount CHECK(amount > 0),
              CONSTRAINT ck_financial_entry_paid CHECK(paid_amount >= 0 AND paid_amount <= amount));
            CREATE INDEX IF NOT EXISTS ix_financial_entry_status_due ON orcafacil.financial_entries(account_id,status,due_date);
            CREATE INDEX IF NOT EXISTS ix_financial_entry_client_due ON orcafacil.financial_entries(account_id,client_id,due_date);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_financial_entry_contract_payment ON orcafacil.financial_entries(account_id,contract_payment_id) WHERE contract_payment_id IS NOT NULL AND is_deleted=false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately non-destructive: financial history must never be dropped automatically.
    }
}
