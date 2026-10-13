using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>
/// The historical check only allowed Free and Pro. Current plans persist Free, Professional, Business and Enterprise.
/// Pro remains accepted so existing rows stay valid. Also repairs critical_route_events when that table was never created.
/// </summary>
public partial class AlignCommercialPlanCheck : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE orcafacil.users DROP CONSTRAINT IF EXISTS ck_orcafacil_users_plan;
        ALTER TABLE orcafacil.users DROP CONSTRAINT IF EXISTS ck_users_plan;
        ALTER TABLE orcafacil.users ADD CONSTRAINT ck_orcafacil_users_plan
            CHECK (plan IN ('Free', 'Pro', 'Professional', 'Business', 'Enterprise'));

        CREATE TABLE IF NOT EXISTS orcafacil.critical_route_events (
            id uuid PRIMARY KEY,
            account_id uuid,
            user_id uuid,
            route varchar(300) NOT NULL,
            status_code integer NOT NULL,
            duration_milliseconds bigint NOT NULL,
            correlation_id varchar(100) NOT NULL,
            error_fingerprint varchar(32),
            sanitized_error varchar(500),
            created_at timestamptz NOT NULL DEFAULT now(),
            updated_at timestamptz,
            is_deleted boolean NOT NULL DEFAULT false
        );
        CREATE INDEX IF NOT EXISTS ix_critical_route_events_account_id_created_at
            ON orcafacil.critical_route_events(account_id, created_at);
        CREATE INDEX IF NOT EXISTS ix_critical_route_events_route_status_code_created_at
            ON orcafacil.critical_route_events(route, status_code, created_at);
        CREATE INDEX IF NOT EXISTS ix_critical_route_events_scope
            ON orcafacil.critical_route_events(account_id, created_at DESC);
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE orcafacil.users DROP CONSTRAINT IF EXISTS ck_orcafacil_users_plan;
        ALTER TABLE orcafacil.users ADD CONSTRAINT ck_orcafacil_users_plan
            CHECK (plan IN ('Free', 'Pro'));
        """);
}
