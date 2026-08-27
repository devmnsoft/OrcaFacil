using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>Non-destructive V4.3 bridge. The complete idempotent schema is database/sprint42_field_operations_v43.sql.</summary>
public partial class AddFieldOperationsV43 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS orcafacil.field_teams (id uuid PRIMARY KEY, account_id uuid NOT NULL, work_order_id uuid NULL, user_id uuid NULL, team_id uuid NULL, status varchar(32) NOT NULL DEFAULT 'Active', data_json jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS orcafacil.field_visit_sessions (id uuid PRIMARY KEY, account_id uuid NOT NULL, work_order_id uuid NOT NULL, user_id uuid NOT NULL, team_id uuid NULL, status varchar(32) NOT NULL DEFAULT 'Open', data_json jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS orcafacil.field_offline_queue_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, work_order_id uuid NULL, user_id uuid NOT NULL, team_id uuid NULL, status varchar(32) NOT NULL DEFAULT 'Pending', data_json jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
            CREATE INDEX IF NOT EXISTS ix_field_teams_account ON orcafacil.field_teams(account_id);
            CREATE INDEX IF NOT EXISTS ix_field_visit_sessions_account ON orcafacil.field_visit_sessions(account_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_field_offline_queue_account_key ON orcafacil.field_offline_queue_items(account_id, ((data_json->>'idempotencyKey'))) WHERE is_deleted = false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("V4.3 preserva a trilha de campo; restaure um backup testado para rollback.");
}
