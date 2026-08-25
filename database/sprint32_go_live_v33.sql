-- Sprint 32 / V3.3. Additive and idempotent: never removes or rewrites customer data.
CREATE SCHEMA IF NOT EXISTS orcafacil;
CREATE TABLE IF NOT EXISTS orcafacil.tenant_provisioning_requests (
 id uuid PRIMARY KEY, account_id uuid NULL, account_name varchar(200) NOT NULL, legal_name varchar(200), document_number varchar(32),
 owner_name varchar(160) NOT NULL, owner_email varchar(254) NOT NULL, owner_phone varchar(32), plan_id uuid NOT NULL,
 trial_days integer NOT NULL DEFAULT 0 CHECK (trial_days BETWEEN 0 AND 90), selected_package_id uuid, status varchar(24) NOT NULL,
 requested_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, error_summary text);
CREATE UNIQUE INDEX IF NOT EXISTS ux_provisioning_account ON orcafacil.tenant_provisioning_requests(account_id) WHERE account_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS orcafacil.tenant_launch_checklists (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(24) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_launch_checklist_account ON orcafacil.tenant_launch_checklists(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_launch_checklist_items (id uuid PRIMARY KEY, checklist_id uuid NOT NULL REFERENCES orcafacil.tenant_launch_checklists(id), code varchar(80) NOT NULL, title varchar(200) NOT NULL, is_automatic boolean NOT NULL, is_blocking boolean NOT NULL, is_optional boolean NOT NULL DEFAULT false, completed_at timestamptz, completed_by_user_id uuid, evidence text, UNIQUE(checklist_id, code));

CREATE TABLE IF NOT EXISTS orcafacil.demo_accounts (id uuid PRIMARY KEY, account_id uuid NOT NULL UNIQUE, block_email boolean NOT NULL DEFAULT true, block_webhook boolean NOT NULL DEFAULT true, block_payment boolean NOT NULL DEFAULT true, block_fiscal boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.demo_account_snapshots (id uuid PRIMARY KEY, demo_account_id uuid NOT NULL REFERENCES orcafacil.demo_accounts(id), payload jsonb NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.demo_account_reset_runs (id uuid PRIMARY KEY, demo_account_id uuid NOT NULL REFERENCES orcafacil.demo_accounts(id), snapshot_id uuid REFERENCES orcafacil.demo_account_snapshots(id), status varchar(24) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz);

CREATE TABLE IF NOT EXISTS orcafacil.customer_implementation_projects (id uuid PRIMARY KEY, account_id uuid NOT NULL, internal_owner_user_id uuid NOT NULL, customer_owner varchar(200), status varchar(32) NOT NULL, current_step varchar(100), planned_go_live_at timestamptz, risks text, pending_items text, notes text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_implementation_account ON orcafacil.customer_implementation_projects(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_migration_batches (id uuid PRIMARY KEY, account_id uuid NOT NULL, migration_type varchar(40) NOT NULL, file_name varchar(255) NOT NULL, status varchar(24) NOT NULL, previewed_at timestamptz, confirmed_by_user_id uuid, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_migration_account ON orcafacil.customer_migration_batches(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_migration_rows (id uuid PRIMARY KEY, batch_id uuid NOT NULL REFERENCES orcafacil.customer_migration_batches(id), row_number integer NOT NULL, source jsonb NOT NULL, status varchar(24) NOT NULL, error_summary text, imported_entity_id uuid, UNIQUE(batch_id,row_number));
CREATE TABLE IF NOT EXISTS orcafacil.tenant_provisioning_steps (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_tenant_provisioning_steps_account ON orcafacil.tenant_provisioning_steps(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_provisioning_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_tenant_provisioning_events_account ON orcafacil.tenant_provisioning_events(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_implementation_steps (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_implementation_steps_account ON orcafacil.customer_implementation_steps(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_implementation_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_implementation_events_account ON orcafacil.customer_implementation_events(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_training_tracks (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_training_tracks_account ON orcafacil.customer_training_tracks(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_training_lessons (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_training_lessons_account ON orcafacil.customer_training_lessons(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_training_progress (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_training_progress_account ON orcafacil.customer_training_progress(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_touchpoints (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_success_touchpoints_account ON orcafacil.customer_success_touchpoints(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_adoption_snapshots (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_adoption_snapshots_account ON orcafacil.customer_adoption_snapshots(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_go_live_reviews (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_go_live_reviews_account ON orcafacil.customer_go_live_reviews(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_launch_notes (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_launch_notes_account ON orcafacil.customer_launch_notes(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_onboarding_sessions (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_customer_onboarding_sessions_account ON orcafacil.customer_onboarding_sessions(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.account_readiness_scores (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_account_readiness_scores_account ON orcafacil.account_readiness_scores(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.account_readiness_findings (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_account_readiness_findings_account ON orcafacil.account_readiness_findings(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.trial_activation_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_trial_activation_events_account ON orcafacil.trial_activation_events(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.trial_conversion_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_trial_conversion_events_account ON orcafacil.trial_conversion_events(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.sales_demo_sessions (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_sales_demo_sessions_account ON orcafacil.sales_demo_sessions(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.sales_demo_activity_logs (id uuid PRIMARY KEY, account_id uuid NOT NULL, status varchar(32) NOT NULL, details jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_sales_demo_activity_logs_account ON orcafacil.sales_demo_activity_logs(account_id);
