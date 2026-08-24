using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddProductivityIntelligence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(@"""

 
CREATE TABLE IF NOT EXISTS orcafacil.recommendation_cards (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid, document_id uuid, public_quote_id uuid, work_order_id uuid, receivable_id uuid, contract_id uuid,
 type varchar(60) NOT NULL, priority varchar(20) NOT NULL, title varchar(180) NOT NULL, description varchar(800) NOT NULL,
 action_label varchar(80) NOT NULL, action_url varchar(400) NOT NULL, reason varchar(800) NOT NULL, status varchar(20) NOT NULL DEFAULT 'Open',
 resolved_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_recommendation_cards_account_status_priority ON orcafacil.recommendation_cards(account_id,status,priority);
CREATE UNIQUE INDEX IF NOT EXISTS ux_recommendation_cards_open_entity ON orcafacil.recommendation_cards(account_id,type,document_id,work_order_id) WHERE is_deleted=false AND status='Open';
CREATE TABLE IF NOT EXISTS orcafacil.automation_rules (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, trigger_type varchar(60) NOT NULL,
 action_type varchar(60) NOT NULL, is_active boolean NOT NULL DEFAULT true, conditions_json text NOT NULL DEFAULT '{}', last_run_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_automation_rules_account_active ON orcafacil.automation_rules(account_id,is_active);
CREATE TABLE IF NOT EXISTS orcafacil.automation_runs (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, automation_rule_id uuid NOT NULL, idempotency_key varchar(200) NOT NULL, status varchar(30) NOT NULL,
 result_summary text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_automation_runs_account_key ON orcafacil.automation_runs(account_id,idempotency_key);
CREATE TABLE IF NOT EXISTS orcafacil.productivity_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid, event_type varchar(60) NOT NULL, entity_id uuid, occurred_at timestamptz NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_productivity_events_account_occurred ON orcafacil.productivity_events(account_id,occurred_at);
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission)
SELECT code,code,false FROM unnest(ARRAY['Recommendations.View','AutomationRules.View','AutomationRules.Manage','Productivity.View','ExecutiveReports.View','Scores.View','Scores.FinancialDetails']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE (r.code IN ('Owner','Administrator') AND p.code IN ('Recommendations.View','AutomationRules.View','AutomationRules.Manage','Productivity.View','ExecutiveReports.View','Scores.View','Scores.FinancialDetails'))
   OR (r.code IN ('Collaborator','Viewer') AND p.code IN ('Recommendations.View','Productivity.View','Scores.View'))
ON CONFLICT(role_id,permission_id) DO NOTHING;
        """);
    protected override void Down(MigrationBuilder migrationBuilder) { /* Operational history is intentionally retained. */ }
}
