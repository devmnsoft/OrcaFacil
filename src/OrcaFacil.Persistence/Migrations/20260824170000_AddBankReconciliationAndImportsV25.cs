using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class AddBankReconciliationAndImportsV25 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS orcafacil.bank_transactions (id uuid PRIMARY KEY, account_id uuid NOT NULL, bank_account_id uuid NOT NULL, transaction_date timestamptz NOT NULL, description varchar(300) NOT NULL, amount numeric(18,2) NOT NULL CHECK(amount > 0), type integer NOT NULL, reference varchar(160), fingerprint char(64) NOT NULL, is_reconciled boolean NOT NULL DEFAULT false, is_deleted boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL);
CREATE UNIQUE INDEX IF NOT EXISTS ux_bank_transactions_fingerprint ON orcafacil.bank_transactions(account_id,fingerprint) WHERE is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.bank_reconciliation_sessions (id uuid PRIMARY KEY, account_id uuid NOT NULL, bank_account_id uuid NOT NULL, status integer NOT NULL, started_at timestamptz NOT NULL, started_by_user_id uuid NOT NULL, completed_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL);
CREATE TABLE IF NOT EXISTS orcafacil.bank_reconciliation_matches (id uuid PRIMARY KEY, account_id uuid NOT NULL, session_id uuid NOT NULL, bank_transaction_id uuid NOT NULL, payable_payment_id uuid, receivable_payment_id uuid, confirmed_at timestamptz NOT NULL, confirmed_by_user_id uuid NOT NULL, reversed_at timestamptz, reversed_by_user_id uuid, reversal_reason text, is_deleted boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, CHECK ((payable_payment_id IS NOT NULL) <> (receivable_payment_id IS NOT NULL)));
CREATE UNIQUE INDEX IF NOT EXISTS ux_reconciliation_active_transaction ON orcafacil.bank_reconciliation_matches(account_id,bank_transaction_id) WHERE reversed_at IS NULL AND is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.financial_import_batches (id uuid PRIMARY KEY, account_id uuid NOT NULL, bank_account_id uuid NOT NULL, file_name varchar(255) NOT NULL, file_hash char(64) NOT NULL, status integer NOT NULL, imported_at timestamptz NOT NULL, imported_by_user_id uuid NOT NULL, is_deleted boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL);
CREATE UNIQUE INDEX IF NOT EXISTS ux_financial_import_batch_hash ON orcafacil.financial_import_batches(account_id,bank_account_id,file_hash) WHERE is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.financial_import_rows (id uuid PRIMARY KEY, account_id uuid NOT NULL, batch_id uuid NOT NULL, row_number integer NOT NULL, transaction_date timestamptz, description varchar(300), amount numeric(18,2), type integer, reference varchar(160), fingerprint char(64), validation_error text, is_duplicate boolean NOT NULL DEFAULT false, bank_transaction_id uuid, is_deleted boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL);
CREATE UNIQUE INDEX IF NOT EXISTS ux_financial_import_row_number ON orcafacil.financial_import_rows(account_id,batch_id,row_number) WHERE is_deleted=false;
""");

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
