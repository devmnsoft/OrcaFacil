using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace OrcaFacil.Persistence.Migrations;
public partial class AddAccountSettings : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("""
 CREATE TABLE IF NOT EXISTS orcafacil.account_settings (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE CASCADE,
 state_registration text, municipal_registration text, whatsapp text, website text, postal_code text, address text, city text, state text, institutional_notes text,
 primary_color varchar(7), secondary_color varchar(7), accent_color varchar(7), logo_path text, compact_logo_path text, visual_signature text, document_footer text, short_institutional_text text,
 default_quote_validity_days integer NOT NULL DEFAULT 15, default_notes text, default_commercial_terms text, default_delivery_term text, default_send_message text,
 quote_prefix varchar(12) NOT NULL DEFAULT 'ORC', work_order_prefix varchar(12) NOT NULL DEFAULT 'OS', receipt_prefix varchar(12) NOT NULL DEFAULT 'REC', show_signature boolean NOT NULL DEFAULT true, show_bank_details boolean NOT NULL DEFAULT false, receipt_notice text,
 follow_up_after_sent_days integer NOT NULL DEFAULT 2, follow_up_after_viewed_days integer NOT NULL DEFAULT 1, expiration_alert_days integer NOT NULL DEFAULT 2, maximum_discount_percent numeric NOT NULL DEFAULT 0, desired_minimum_margin_percent numeric, discount_policy text, whatsapp_message text, email_message text, default_loss_reason text,
 accepted_payment_methods text, bank_name text, bank_branch text, bank_account text, beneficiary text, pix_key text, payment_instructions text, receipt_text text, collection_message text, notification_preferences_json jsonb NOT NULL DEFAULT '{}',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
 CREATE UNIQUE INDEX IF NOT EXISTS ix_account_settings_account_id ON orcafacil.account_settings(account_id);
 """);
 protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("DROP TABLE IF EXISTS orcafacil.account_settings;");
}
