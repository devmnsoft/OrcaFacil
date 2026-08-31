-- Sprint 52 / V5.3 - governança de dados. Seguro para bancos novos e existentes.
CREATE SCHEMA IF NOT EXISTS orcafacil;

CREATE TABLE IF NOT EXISTS orcafacil.data_quality_rules (
 id uuid PRIMARY KEY, account_id uuid NULL, code varchar(100) NOT NULL, name varchar(180) NOT NULL,
 description text NULL, entity_type varchar(80) NOT NULL, evaluated_field varchar(100) NULL,
 severity varchar(20) NOT NULL, recommended_action text NOT NULL, is_active boolean NOT NULL DEFAULT true,
 blocks_flow boolean NOT NULL DEFAULT false, is_global boolean NOT NULL DEFAULT false,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false,
 CONSTRAINT ck_data_quality_rule_scope CHECK ((is_global AND account_id IS NULL) OR (NOT is_global AND account_id IS NOT NULL)));
CREATE UNIQUE INDEX IF NOT EXISTS ux_data_quality_rules_scope_code ON orcafacil.data_quality_rules(COALESCE(account_id,'00000000-0000-0000-0000-000000000000'::uuid),code) WHERE NOT is_deleted;

CREATE TABLE IF NOT EXISTS orcafacil.data_quality_checks (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(24) NOT NULL, started_at timestamptz NOT NULL,
 completed_at timestamptz NULL, evaluated_records integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_data_quality_checks_account_started ON orcafacil.data_quality_checks(account_id,started_at DESC);

CREATE TABLE IF NOT EXISTS orcafacil.data_quality_findings_v53 (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, check_id uuid NULL, rule_id uuid NULL, entity_type varchar(80) NOT NULL,
 entity_id uuid NOT NULL, severity varchar(20) NOT NULL, status varchar(24) NOT NULL DEFAULT 'Open',
 message text NOT NULL, recommendation text NULL, resolution_reason text NULL, detected_at timestamptz NOT NULL DEFAULT now(),
 resolved_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_findings_v53_account_status ON orcafacil.data_quality_findings_v53(account_id,status,severity);

CREATE TABLE IF NOT EXISTS orcafacil.master_data_merge_candidates (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, entity_type varchar(80) NOT NULL, source_entity_id uuid NOT NULL,
 target_entity_id uuid NOT NULL, similarity integer NOT NULL, matched_fields_json jsonb NOT NULL DEFAULT '[]', status varchar(24) NOT NULL DEFAULT 'Pending',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false,
 CONSTRAINT ck_merge_distinct_records CHECK(source_entity_id <> target_entity_id), CONSTRAINT ck_merge_similarity CHECK(similarity BETWEEN 0 AND 100));
CREATE UNIQUE INDEX IF NOT EXISTS ux_merge_candidate_pair ON orcafacil.master_data_merge_candidates(account_id,entity_type,source_entity_id,target_entity_id) WHERE NOT is_deleted;

CREATE TABLE IF NOT EXISTS orcafacil.master_data_merge_reviews (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, candidate_id uuid NOT NULL, primary_entity_id uuid NOT NULL,
 secondary_entity_id uuid NOT NULL, preview_json jsonb NOT NULL, conflict_resolution_json jsonb NOT NULL DEFAULT '{}',
 reason text NULL, reviewed_by_user_id uuid NULL, confirmed_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_merge_reviews_account_candidate ON orcafacil.master_data_merge_reviews(account_id,candidate_id);

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
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_rule_versions (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_rule_versions_account_created ON orcafacil.data_quality_rule_versions(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_rule_scopes (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_rule_scopes_account_created ON orcafacil.data_quality_rule_scopes(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_check_items (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_check_items_account_created ON orcafacil.data_quality_check_items(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_finding_events (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_finding_events_account_created ON orcafacil.data_quality_finding_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_scores (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_scores_account_created ON orcafacil.data_quality_scores(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_score_snapshots (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_score_snapshots_account_created ON orcafacil.data_quality_score_snapshots(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_fix_suggestions (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_fix_suggestions_account_created ON orcafacil.data_quality_fix_suggestions(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_fix_actions (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_fix_actions_account_created ON orcafacil.data_quality_fix_actions(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_fix_approvals (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_quality_fix_approvals_account_created ON orcafacil.data_quality_fix_approvals(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.master_data_entities (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_master_data_entities_account_created ON orcafacil.master_data_entities(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.master_data_entity_keys (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_master_data_entity_keys_account_created ON orcafacil.master_data_entity_keys(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.master_data_normalization_rules (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_master_data_normalization_rules_account_created ON orcafacil.master_data_normalization_rules(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.master_data_normalization_events (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_master_data_normalization_events_account_created ON orcafacil.master_data_normalization_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_profiles (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_profiles_account_created ON orcafacil.data_import_profiles(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_templates (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_templates_account_created ON orcafacil.data_import_templates(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_batches (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_batches_account_created ON orcafacil.data_import_batches(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_rows (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_rows_account_created ON orcafacil.data_import_rows(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_row_errors (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_row_errors_account_created ON orcafacil.data_import_row_errors(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_column_mappings (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_column_mappings_account_created ON orcafacil.data_import_column_mappings(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_commits (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_commits_account_created ON orcafacil.data_import_commits(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_import_audit_events (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_import_audit_events_account_created ON orcafacil.data_import_audit_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.duplicate_detection_rules (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_duplicate_detection_rules_account_created ON orcafacil.duplicate_detection_rules(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.duplicate_detection_runs (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_duplicate_detection_runs_account_created ON orcafacil.duplicate_detection_runs(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.duplicate_detection_candidates (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_duplicate_detection_candidates_account_created ON orcafacil.duplicate_detection_candidates(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.duplicate_detection_decisions (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_duplicate_detection_decisions_account_created ON orcafacil.duplicate_detection_decisions(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_integrity_constraints (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_integrity_constraints_account_created ON orcafacil.data_integrity_constraints(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_integrity_check_runs (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_integrity_check_runs_account_created ON orcafacil.data_integrity_check_runs(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.data_integrity_check_items (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_integrity_check_items_account_created ON orcafacil.data_integrity_check_items(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.sensitive_data_change_reviews (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_sensitive_data_change_reviews_account_created ON orcafacil.sensitive_data_change_reviews(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.sensitive_data_change_events (
 id uuid PRIMARY KEY, account_id uuid NULL, aggregate_id uuid NULL, entity_type varchar(80) NULL, entity_id uuid NULL,
 status varchar(32) NOT NULL DEFAULT 'Active', payload_json jsonb NOT NULL DEFAULT '{}', actor_user_id uuid NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL,
 is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_sensitive_data_change_events_account_created ON orcafacil.sensitive_data_change_events(account_id,created_at DESC);
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission)
SELECT code,code,false FROM unnest(ARRAY['DataQuality.View','DataQuality.Manage','DataQuality.RulesView','DataQuality.RulesManage','DataQuality.FindingsView','DataQuality.ResolveFindings','DataQuality.DuplicatesView','DataQuality.Merge','DataQuality.Normalize','DataImport.View','DataImport.Manage','DataImport.Commit','DataImport.Rollback','DataIntegrity.View','SensitiveDataReview.View','SensitiveDataReview.Manage','DataQuality.ReportsView','DataQuality.Export']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE r.code IN ('Owner','Administrator') AND p.code = ANY(ARRAY['DataQuality.View','DataQuality.Manage','DataQuality.RulesView','DataQuality.RulesManage','DataQuality.FindingsView','DataQuality.ResolveFindings','DataQuality.DuplicatesView','DataQuality.Merge','DataQuality.Normalize','DataImport.View','DataImport.Manage','DataImport.Commit','DataImport.Rollback','DataIntegrity.View','SensitiveDataReview.View','SensitiveDataReview.Manage','DataQuality.ReportsView','DataQuality.Export'])
ON CONFLICT(role_id,permission_id) DO NOTHING;
