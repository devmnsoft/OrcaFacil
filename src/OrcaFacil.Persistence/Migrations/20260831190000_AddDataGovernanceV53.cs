using Microsoft.EntityFrameworkCore.Migrations;

namespace OrcaFacil.Persistence.Migrations;

public partial class AddDataGovernanceV53 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE IF NOT EXISTS orcafacil.data_quality_rules (
          id uuid PRIMARY KEY, account_id uuid NULL, code varchar(100) NOT NULL, name varchar(180) NOT NULL,
          description text NULL, entity_type varchar(80) NOT NULL, evaluated_field varchar(100) NULL,
          severity varchar(20) NOT NULL, recommended_action text NOT NULL, is_active boolean NOT NULL DEFAULT true,
          blocks_flow boolean NOT NULL DEFAULT false, is_global boolean NOT NULL DEFAULT false,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false,
          CONSTRAINT ck_data_quality_rule_scope CHECK ((is_global AND account_id IS NULL) OR (NOT is_global AND account_id IS NOT NULL)));
        CREATE UNIQUE INDEX IF NOT EXISTS ux_data_quality_rules_scope_code ON orcafacil.data_quality_rules(COALESCE(account_id,'00000000-0000-0000-0000-000000000000'::uuid),code) WHERE NOT is_deleted;
        CREATE TABLE IF NOT EXISTS orcafacil.master_data_merge_candidates (
          id uuid PRIMARY KEY, account_id uuid NOT NULL, entity_type varchar(80) NOT NULL, source_entity_id uuid NOT NULL,
          target_entity_id uuid NOT NULL, similarity integer NOT NULL, matched_fields_json jsonb NOT NULL DEFAULT '[]',
          status varchar(24) NOT NULL DEFAULT 'Pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
          is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT ck_merge_distinct_records CHECK(source_entity_id <> target_entity_id),
          CONSTRAINT ck_merge_similarity CHECK(similarity BETWEEN 0 AND 100));
        CREATE INDEX IF NOT EXISTS ix_merge_candidates_account ON orcafacil.master_data_merge_candidates(account_id,status);
        CREATE TABLE IF NOT EXISTS orcafacil.master_data_merge_events (
          id uuid PRIMARY KEY, account_id uuid NOT NULL, review_id uuid NOT NULL, event_type varchar(40) NOT NULL,
          original_snapshot_json jsonb NOT NULL, resulting_snapshot_json jsonb NULL, actor_user_id uuid NOT NULL,
          occurred_at timestamptz NOT NULL DEFAULT now());
        CREATE INDEX IF NOT EXISTS ix_merge_events_account_review ON orcafacil.master_data_merge_events(account_id,review_id,occurred_at);
        CREATE TABLE IF NOT EXISTS orcafacil.data_import_previews (
          id uuid PRIMARY KEY, account_id uuid NOT NULL, batch_id uuid NOT NULL, preview_token uuid NOT NULL,
          rows_json jsonb NOT NULL, error_count integer NOT NULL DEFAULT 0, expires_at timestamptz NOT NULL,
          confirmed_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now());
        CREATE UNIQUE INDEX IF NOT EXISTS ux_data_import_preview_token ON orcafacil.data_import_previews(account_id,preview_token);
        CREATE TABLE IF NOT EXISTS orcafacil.data_import_rollback_points (
          id uuid PRIMARY KEY, account_id uuid NOT NULL, batch_id uuid NOT NULL, commit_id uuid NOT NULL,
          snapshot_json jsonb NOT NULL, committed_at timestamptz NOT NULL, invalidated_at timestamptz NULL,
          invalidation_reason text NULL, rolled_back_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now());
        CREATE INDEX IF NOT EXISTS ix_import_rollback_account_batch ON orcafacil.data_import_rollback_points(account_id,batch_id);
        INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission)
        SELECT code,code,false FROM unnest(ARRAY['DataQuality.View','DataQuality.Manage','DataQuality.RulesView','DataQuality.RulesManage','DataQuality.FindingsView','DataQuality.ResolveFindings','DataQuality.DuplicatesView','DataQuality.Merge','DataQuality.Normalize','DataImport.View','DataImport.Manage','DataImport.Commit','DataImport.Rollback','DataIntegrity.View','SensitiveDataReview.View','SensitiveDataReview.Manage','DataQuality.ReportsView','DataQuality.Export']) code ON CONFLICT(code) DO NOTHING;
        INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
        SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
        WHERE r.code IN ('Owner','Administrator') AND (p.code LIKE 'DataQuality.%' OR p.code LIKE 'DataImport.%' OR p.code IN ('DataIntegrity.View','SensitiveDataReview.View','SensitiveDataReview.Manage'))
        ON CONFLICT(role_id,permission_id) DO NOTHING;
        """);

    // A governança é deliberadamente preservada em rollback: registros de auditoria e
    // snapshots de mesclagem/importação nunca são removidos automaticamente.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
