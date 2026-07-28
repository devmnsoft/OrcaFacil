using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>
/// Brings databases created only through EF migrations to the same registration schema as the
/// consolidated SQL script. All statements are additive so an installation previously prepared
/// by the script can safely apply this migration.
/// </summary>
public partial class StabilizeRegistrationSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS orcafacil.business_accounts (
                id uuid PRIMARY KEY, display_name varchar(180) NOT NULL, legal_name varchar(180),
                trade_name varchar(180), person_type varchar(30) NOT NULL, document_type varchar(20),
                document_number varchar(20), email varchar(254) NOT NULL, phone varchar(40),
                status varchar(30) NOT NULL DEFAULT 'Active', current_plan_code varchar(40) NOT NULL DEFAULT 'FREE',
                created_at timestamptz NOT NULL, updated_at timestamptz, activated_at timestamptz,
                deactivated_at timestamptz, blocked_at timestamptz, block_reason varchar(500),
                is_deleted boolean NOT NULL DEFAULT false);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_business_accounts_document_number
                ON orcafacil.business_accounts(document_number) WHERE document_number IS NOT NULL AND is_deleted = false;

            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.issuer_profiles ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.audit_logs ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.audit_logs ADD COLUMN IF NOT EXISTS account_id uuid;

            CREATE TABLE IF NOT EXISTS orcafacil.account_members (
                id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id),
                user_id uuid NOT NULL REFERENCES orcafacil.users(id), role_code varchar(80) NOT NULL,
                status varchar(30) NOT NULL, invited_at timestamptz, joined_at timestamptz,
                disabled_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz,
                is_deleted boolean NOT NULL DEFAULT false, UNIQUE(account_id, user_id));

            CREATE TABLE IF NOT EXISTS orcafacil.plans (
                id uuid PRIMARY KEY, code varchar(40) NOT NULL UNIQUE, display_name varchar(100) NOT NULL,
                short_description varchar(300) NOT NULL, is_free boolean NOT NULL DEFAULT false,
                is_active boolean NOT NULL DEFAULT true, is_public boolean NOT NULL DEFAULT true,
                is_recommended boolean NOT NULL DEFAULT false, display_order integer NOT NULL DEFAULT 0,
                created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS orcafacil.plan_versions (
                id uuid PRIMARY KEY, plan_id uuid NOT NULL REFERENCES orcafacil.plans(id), version_number integer NOT NULL,
                monthly_price numeric(12,2) NOT NULL, annual_price numeric(12,2) NOT NULL, currency char(3) NOT NULL DEFAULT 'BRL',
                trial_days integer NOT NULL DEFAULT 0, grace_period_days integer NOT NULL DEFAULT 0,
                valid_from timestamptz NOT NULL, valid_until timestamptz, status varchar(30) NOT NULL,
                published_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz,
                is_deleted boolean NOT NULL DEFAULT false, UNIQUE(plan_id, version_number));

            CREATE TABLE IF NOT EXISTS orcafacil.billing_customer_profiles (
                id uuid PRIMARY KEY, user_id uuid NOT NULL REFERENCES orcafacil.users(id),
                account_id uuid REFERENCES orcafacil.business_accounts(id), person_type varchar(30) NOT NULL,
                document_type varchar(10), document_number varchar(20), name varchar(180) NOT NULL,
                trade_name varchar(180), legal_name varchar(180), email varchar(254), phone varchar(40),
                city varchar(120), state varchar(2), postal_code varchar(8), street varchar(180),
                street_number varchar(30), complement varchar(120), district varchar(120), address varchar(300),
                mercado_pago_customer_id varchar(180), created_at timestamptz NOT NULL, updated_at timestamptz,
                is_deleted boolean NOT NULL DEFAULT false);

            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS account_id uuid REFERENCES orcafacil.business_accounts(id);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS state varchar(2);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS postal_code varchar(8);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS street varchar(180);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS street_number varchar(30);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS complement varchar(120);
            ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS district varchar(120);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_billing_profiles_account_id ON orcafacil.billing_customer_profiles(account_id) WHERE account_id IS NOT NULL AND is_deleted = false;

            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS account_id uuid REFERENCES orcafacil.business_accounts(id);
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS selected_plan_version_id uuid REFERENCES orcafacil.plan_versions(id);
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS effective_plan_version_id uuid REFERENCES orcafacil.plan_versions(id);
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS price_at_activation numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS paid_through_at timestamptz;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS next_due_at timestamptz;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS past_due_since timestamptz;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS suspended_at timestamptz;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS manual_release_until timestamptz;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_started_at timestamptz;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_ends_at timestamptz;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_used boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_status varchar(30) NOT NULL DEFAULT 'NotStarted';

            ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS account_id uuid REFERENCES orcafacil.business_accounts(id);
            ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS category varchar(30) NOT NULL DEFAULT 'System';
            ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS action_url varchar(400);
            ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS action_text varchar(80);
            ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS read_at timestamptz;
            ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS is_read boolean NOT NULL DEFAULT false;

            INSERT INTO orcafacil.plans (id, code, display_name, short_description, is_free, is_active, is_public,
                is_recommended, display_order, created_at, is_deleted)
            VALUES (md5('FREE')::uuid, 'FREE', 'Grátis', 'Para começar a organizar seus documentos.', true, true, true, false, 1, now(), false)
            ON CONFLICT (code) DO NOTHING;
            INSERT INTO orcafacil.plan_versions (id, plan_id, version_number, monthly_price, annual_price, currency,
                trial_days, grace_period_days, valid_from, status, published_at, created_at, is_deleted)
            SELECT md5('FREE:v1')::uuid, id, 1, 0, 0, 'BRL', 0, 0, now(), 'Published', now(), now(), false
              FROM orcafacil.plans WHERE code = 'FREE'
            ON CONFLICT (plan_id, version_number) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Production-safe additive stabilization; destructive rollback is intentionally omitted.
    }
}
