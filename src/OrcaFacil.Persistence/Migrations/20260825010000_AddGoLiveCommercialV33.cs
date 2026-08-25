using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>Additive Sprint 32 entry point. The complete, idempotent DBA rollout is database/sprint32_go_live_v33.sql.</summary>
public partial class AddGoLiveCommercialV33 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS orcafacil.tenant_provisioning_requests (
              id uuid PRIMARY KEY, account_id uuid NULL, account_name varchar(200) NOT NULL,
              legal_name varchar(200), document_number varchar(32), owner_name varchar(160) NOT NULL,
              owner_email varchar(254) NOT NULL, owner_phone varchar(32), plan_id uuid NOT NULL,
              trial_days integer NOT NULL DEFAULT 0 CHECK (trial_days BETWEEN 0 AND 90),
              selected_package_id uuid, status varchar(24) NOT NULL, requested_by_user_id uuid NOT NULL,
              created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, error_summary text);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_provisioning_account
              ON orcafacil.tenant_provisioning_requests(account_id) WHERE account_id IS NOT NULL;
            CREATE TABLE IF NOT EXISTS orcafacil.demo_accounts (
              id uuid PRIMARY KEY, account_id uuid NOT NULL UNIQUE,
              block_email boolean NOT NULL DEFAULT true, block_webhook boolean NOT NULL DEFAULT true,
              block_payment boolean NOT NULL DEFAULT true, block_fiscal boolean NOT NULL DEFAULT true,
              created_at timestamptz NOT NULL DEFAULT now());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive. Production rollback retains Sprint 32 customer and audit data.
    }
}
