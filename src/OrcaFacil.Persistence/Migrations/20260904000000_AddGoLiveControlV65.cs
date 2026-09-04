using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace OrcaFacil.Persistence.Migrations;
public partial class AddGoLiveControlV65 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS orcafacil.go_live_account_states (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, pilot_started_at timestamptz, live_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_go_live_account_states_account ON orcafacil.go_live_account_states(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.go_live_checklist_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, code varchar(80) NOT NULL, title varchar(180) NOT NULL, is_critical boolean NOT NULL, is_automatic boolean NOT NULL, is_completed boolean NOT NULL, completed_by_user_id uuid, completed_at timestamptz, responsible_name varchar(160), observation varchar(2000), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_go_live_items_account_code ON orcafacil.go_live_checklist_items(account_id,code);
CREATE TABLE IF NOT EXISTS orcafacil.training_progress (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NOT NULL, lesson_code varchar(80) NOT NULL, completed_at timestamptz, user_confirmed boolean NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_training_progress_scope ON orcafacil.training_progress(account_id,user_id,lesson_code);
CREATE TABLE IF NOT EXISTS orcafacil.critical_route_events (id uuid PRIMARY KEY, account_id uuid, user_id uuid, route varchar(300) NOT NULL, status_code integer NOT NULL, duration_milliseconds bigint NOT NULL, correlation_id varchar(100) NOT NULL, error_fingerprint varchar(32), sanitized_error varchar(500), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_critical_route_events_scope ON orcafacil.critical_route_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.assisted_operation_actions (id uuid PRIMARY KEY, account_id uuid NOT NULL, created_by_user_id uuid NOT NULL, title varchar(180) NOT NULL, notes varchar(2000) NOT NULL, due_at timestamptz NOT NULL, completed_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_assisted_actions_scope ON orcafacil.assisted_operation_actions(account_id,completed_at,due_at);
""");
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
