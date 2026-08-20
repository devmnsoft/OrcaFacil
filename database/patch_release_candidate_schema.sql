-- OrçaFácil RC: atualização exclusivamente aditiva para instalações existentes.
-- Pode ser executada repetidamente. Não remove tabelas, dados, usuários ou senhas.
\set ON_ERROR_STOP on
BEGIN;

CREATE SCHEMA IF NOT EXISTS orcafacil;

-- O script completo cria a tabela. Este patch falha de forma clara quando uma
-- instalação incompleta não possui users, evitando criar uma tabela parcial.
DO $rc$
BEGIN
  IF to_regclass('orcafacil.users') IS NULL THEN
    RAISE EXCEPTION 'Banco desatualizado: execute database/script_completop.sql antes deste patch.';
  END IF;
END $rc$;

ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS failed_login_attempts integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_failed_login_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_successful_login_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS locked_until timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS is_blocked boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS block_reason varchar(500);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_by_user_id uuid;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_expires_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_reset_reason varchar(500);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_privacy_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_terms_at timestamptz;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS legacy_unversioned_acceptance boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS ix_users_locked_until
  ON orcafacil.users (locked_until) WHERE locked_until IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_users_blocked
  ON orcafacil.users (is_blocked) WHERE is_blocked = true;


-- Onboarding por conta (correção aditiva e segura para reexecução).
CREATE TABLE IF NOT EXISTS orcafacil.account_onboarding_states (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    account_id uuid NOT NULL,
    user_id uuid NOT NULL,
    current_step varchar(32) NOT NULL DEFAULT 'Welcome',
    business_profile_completed_at timestamptz,
    issuer_profile_completed_at timestamptz,
    first_client_completed_at timestamptz,
    first_service_completed_at timestamptz,
    first_budget_started_at timestamptz,
    first_budget_completed_at timestamptz,
    completed_at timestamptz,
    skipped_at timestamptz,
    last_seen_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_account_onboarding_states PRIMARY KEY (id)
);

-- Repara instalações em que a tabela foi criada parcialmente. As colunas de
-- vínculo permanecem sem valor padrão para nunca associar dados à conta errada.
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS id uuid DEFAULT gen_random_uuid();
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS account_id uuid;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS user_id uuid;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS current_step varchar(32) NOT NULL DEFAULT 'Welcome';
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS business_profile_completed_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS issuer_profile_completed_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS first_client_completed_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS first_service_completed_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS first_budget_started_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS first_budget_completed_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS completed_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS skipped_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS last_seen_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE orcafacil.account_onboarding_states ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

CREATE UNIQUE INDEX IF NOT EXISTS ix_account_onboarding_states_account_id_user_id
    ON orcafacil.account_onboarding_states (account_id, user_id);
CREATE INDEX IF NOT EXISTS ix_account_onboarding_states_current_step_last_seen_at
    ON orcafacil.account_onboarding_states (current_step, last_seen_at);

COMMIT;

SELECT CASE WHEN count(*) = 15 THEN 'RC schema de autenticação atualizado'
            ELSE 'Banco desatualizado: execute o script de atualização antes de continuar.' END AS summary
FROM information_schema.columns
WHERE table_schema = 'orcafacil' AND table_name = 'users'
  AND column_name = ANY (ARRAY[
    'failed_login_attempts','last_failed_login_at','last_successful_login_at','locked_until','is_blocked',
    'block_reason','must_change_password','password_changed_at','password_changed_by_user_id','password_expires_at',
    'password_reset_reason','session_version','accepted_privacy_at','accepted_terms_at','legacy_unversioned_acceptance']);

-- Go-live assistido V1: suporte contextual, feedback e conteúdo administrável.
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS related_page varchar(300);
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS correlation_id varchar(100);
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS browser_info varchar(500);
CREATE TABLE IF NOT EXISTS orcafacil.user_feedback (id uuid PRIMARY KEY, account_id uuid NULL, user_id uuid NULL, page_url varchar(500) NOT NULL, rating varchar(32) NOT NULL, message varchar(2000), browser_info varchar(500), correlation_id varchar(100), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_user_feedback_account_created ON orcafacil.user_feedback(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.knowledge_base_articles (id uuid PRIMARY KEY, title varchar(180) NOT NULL, slug varchar(180) NOT NULL, summary varchar(500) NOT NULL, content varchar(12000) NOT NULL, category varchar(80) NOT NULL, audience varchar(24) NOT NULL DEFAULT 'All', is_published boolean NOT NULL DEFAULT false, display_order integer NOT NULL DEFAULT 0, published_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_knowledge_base_articles_slug ON orcafacil.knowledge_base_articles(slug);
CREATE TABLE IF NOT EXISTS orcafacil.release_notes (id uuid PRIMARY KEY, version varchar(30) NOT NULL, title varchar(180) NOT NULL, description varchar(5000) NOT NULL, released_at timestamptz NOT NULL, category varchar(32) NOT NULL, is_published boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_release_notes_published_release ON orcafacil.release_notes(is_published,released_at DESC);
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['Support.View','Support.CreateTicket','Support.ManageTickets','Feedback.View','Feedback.Create','KnowledgeBase.Manage','ReleaseNotes.Manage','SetupChecklist.View','SetupChecklist.Manage','Admin.Access']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE (r.code IN ('Owner','Administrator') AND p.code IN ('Support.View','Support.CreateTicket','Feedback.Create','SetupChecklist.View','SetupChecklist.Manage')) OR (r.code IN ('Collaborator','Viewer') AND p.code IN ('Support.View','Support.CreateTicket','Feedback.Create','SetupChecklist.View')) OR (r.code IN ('SuperAdministrator','PlatformSupport') AND p.code IN ('Support.View','Support.CreateTicket','Support.ManageTickets','Feedback.View','Feedback.Create','KnowledgeBase.Manage','ReleaseNotes.Manage','SetupChecklist.View','SetupChecklist.Manage','Admin.Access')) ON CONFLICT(role_id,permission_id) DO NOTHING;
-- Financeiro V1.2: contas a receber (aditivo idempotente, sem remoção de dados)
BEGIN;
CREATE TABLE IF NOT EXISTS orcafacil.financial_entries (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, document_id uuid, work_order_id uuid, contract_id uuid, contract_payment_id uuid,
 origin varchar(24) NOT NULL, description varchar(500) NOT NULL, due_date date NOT NULL, amount numeric(18,2) NOT NULL, paid_amount numeric(18,2) NOT NULL DEFAULT 0,
 status varchar(24) NOT NULL DEFAULT 'Pending', canceled_at timestamptz, canceled_by_user_id uuid, cancellation_reason varchar(500),
 created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
 CONSTRAINT ck_financial_entry_amount CHECK(amount > 0), CONSTRAINT ck_financial_entry_paid CHECK(paid_amount >= 0 AND paid_amount <= amount)
);
CREATE INDEX IF NOT EXISTS ix_financial_entry_status_due ON orcafacil.financial_entries(account_id,status,due_date);
CREATE INDEX IF NOT EXISTS ix_financial_entry_client_due ON orcafacil.financial_entries(account_id,client_id,due_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_financial_entry_contract_payment ON orcafacil.financial_entries(account_id,contract_payment_id) WHERE contract_payment_id IS NOT NULL AND is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.receipt_sequences (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, year integer NOT NULL, current_number bigint NOT NULL DEFAULT 0,
 prefix varchar(12) NOT NULL DEFAULT 'REC', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_receipt_sequences_account_year ON orcafacil.receipt_sequences(account_id,year);
INSERT INTO orcafacil.receipt_sequences(id,account_id,year,current_number,prefix,created_at,is_deleted)
SELECT gen_random_uuid(),account_id,EXTRACT(YEAR FROM issued_at)::integer,COUNT(*),'REC',now(),false FROM orcafacil.receipts WHERE is_deleted=false
GROUP BY account_id,EXTRACT(YEAR FROM issued_at)::integer ON CONFLICT(account_id,year) DO UPDATE SET current_number=GREATEST(orcafacil.receipt_sequences.current_number,EXCLUDED.current_number),updated_at=now();
COMMIT;

-- Sprint 3: motivo obrigatório de cancelamento (evolução não destrutiva e idempotente).
ALTER TABLE orcafacil.work_orders ADD COLUMN IF NOT EXISTS cancellation_reason varchar(1000);

-- Sprint 11 / V1.2: inteligência produtiva baseada exclusivamente em dados da conta.
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

-- Sprint 12 / V1.3: connectivity records are tenant-owned and secrets are protected or hashed.
CREATE TABLE IF NOT EXISTS orcafacil.integration_settings (id uuid PRIMARY KEY,account_id uuid NOT NULL,public_base_url varchar(500),whats_app_number varchar(30),email_signature text,support_email varchar(255),smtp_host varchar(255),smtp_port integer,smtp_user varchar(255),protected_smtp_password text,smtp_use_ssl boolean NOT NULL DEFAULT true,email_sending_enabled boolean NOT NULL DEFAULT false,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_integration_settings_account_id ON orcafacil.integration_settings(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.webhook_endpoints (id uuid PRIMARY KEY,account_id uuid NOT NULL,name varchar(120) NOT NULL,url varchar(1000) NOT NULL,secret_hash varchar(64) NOT NULL,protected_secret text NOT NULL,event_types text NOT NULL,is_active boolean NOT NULL DEFAULT true,last_delivery_at timestamptz,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_webhook_endpoints_account_active ON orcafacil.webhook_endpoints(account_id,is_active);
CREATE TABLE IF NOT EXISTS orcafacil.webhook_deliveries (id uuid PRIMARY KEY,account_id uuid NOT NULL,webhook_endpoint_id uuid NOT NULL,event_id uuid NOT NULL,event_type varchar(80) NOT NULL,entity_type text NOT NULL,entity_id text NOT NULL,payload_json text NOT NULL DEFAULT '{}',idempotency_key varchar(180) NOT NULL,status varchar(20) NOT NULL,attempts integer NOT NULL DEFAULT 0,next_attempt_at timestamptz NOT NULL DEFAULT now(),delivered_at timestamptz,last_error_summary text,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_webhook_deliveries_idempotency_key ON orcafacil.webhook_deliveries(idempotency_key);
CREATE INDEX IF NOT EXISTS ix_webhook_deliveries_account_status_next ON orcafacil.webhook_deliveries(account_id,status,next_attempt_at);
CREATE TABLE IF NOT EXISTS orcafacil.api_keys (id uuid PRIMARY KEY,account_id uuid NOT NULL,name varchar(120) NOT NULL,key_hash varchar(64) NOT NULL,prefix varchar(20) NOT NULL,scopes varchar(500) NOT NULL,last_used_at timestamptz,expires_at timestamptz,revoked_at timestamptz,created_by_user_id uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_api_keys_key_hash ON orcafacil.api_keys(key_hash);
CREATE INDEX IF NOT EXISTS ix_api_keys_account_revoked ON orcafacil.api_keys(account_id,revoked_at);
CREATE TABLE IF NOT EXISTS orcafacil.data_exports (id uuid PRIMARY KEY,account_id uuid NOT NULL,requested_by_user_id uuid NOT NULL,data_type varchar(40) NOT NULL,format varchar(10) NOT NULL,row_count integer NOT NULL,completed_at timestamptz NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_data_exports_account_completed ON orcafacil.data_exports(account_id,completed_at);
ALTER TABLE orcafacil.account_settings ADD COLUMN IF NOT EXISTS communication_preferences_json jsonb NOT NULL DEFAULT '{}';
ALTER TABLE orcafacil.email_outbox_messages ADD COLUMN IF NOT EXISTS account_id uuid;
ALTER TABLE orcafacil.email_outbox_messages ADD COLUMN IF NOT EXISTS last_error_summary text;
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['Integrations.View','Integrations.Manage','Webhooks.View','Webhooks.Manage','ApiKeys.Manage','Imports.Manage','Exports.Manage','Notifications.Manage','CommunicationPreferences.Manage']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted) SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE r.code IN ('Owner','Administrator') AND p.code IN ('Integrations.View','Integrations.Manage','Webhooks.View','Webhooks.Manage','ApiKeys.Manage','Imports.Manage','Exports.Manage','Notifications.Manage','CommunicationPreferences.Manage') ON CONFLICT(role_id,permission_id) DO NOTHING;

-- Sprint 13 / V1.4: professional documents and private file metadata (non-destructive).
CREATE TABLE IF NOT EXISTS orcafacil.file_assets (id uuid PRIMARY KEY, account_id uuid NOT NULL, uploaded_by_user_id uuid NOT NULL, original_file_name varchar(255) NOT NULL, stored_file_name varchar(80) NOT NULL, storage_path varchar(500) NOT NULL, content_type varchar(120) NOT NULL, extension varchar(12) NOT NULL, size_in_bytes bigint NOT NULL, sha256_hash varchar(64) NOT NULL, category integer NOT NULL, visibility integer NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_file_assets_account_created ON orcafacil.file_assets(account_id,created_at);
CREATE TABLE IF NOT EXISTS orcafacil.file_asset_links (id uuid PRIMARY KEY, account_id uuid NOT NULL, file_asset_id uuid NOT NULL REFERENCES orcafacil.file_assets(id) ON DELETE RESTRICT, entity_type varchar(40) NOT NULL, entity_id uuid NOT NULL, visibility integer NOT NULL, created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.company_branding_profiles (id uuid PRIMARY KEY, account_id uuid NOT NULL UNIQUE, logo_file_asset_id uuid NULL REFERENCES orcafacil.file_assets(id) ON DELETE SET NULL, trade_name varchar(160) NOT NULL, legal_name text NULL, document_number text NULL, phone text NULL, whats_app text NULL, commercial_email text NULL, website text NULL, address text NULL, primary_color varchar(7) NOT NULL, secondary_color varchar(7) NOT NULL, default_footer text NULL, default_commercial_notes text NULL, visual_signature text NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.document_templates (id uuid PRIMARY KEY, account_id uuid NULL, name varchar(160) NOT NULL, type integer NOT NULL, is_default boolean NOT NULL, is_active boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.document_template_versions (id uuid PRIMARY KEY, template_id uuid NOT NULL REFERENCES orcafacil.document_templates(id) ON DELETE RESTRICT, version_number integer NOT NULL, content text NOT NULL, variables_json jsonb NOT NULL DEFAULT '[]', published_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(template_id,version_number));
CREATE TABLE IF NOT EXISTS orcafacil.document_audit_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NULL, event_type varchar(80) NOT NULL, entity_type varchar(40) NOT NULL, entity_id uuid NOT NULL, metadata_json jsonb NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['Files.View','Files.Upload','Files.Download','Files.Delete','DocumentTemplates.View','DocumentTemplates.Manage','Documents.Print','Documents.ExportPdf','Receipts.Print','Receipts.ExportPdf','WorkOrders.Print','Contracts.Print','Branding.Manage']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted) SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE r.code IN ('Owner','Administrator') AND p.code LIKE ANY(ARRAY['Files.%','DocumentTemplates.%','Documents.%','Receipts.Print','Receipts.ExportPdf','WorkOrders.Print','Contracts.Print','Branding.Manage']) ON CONFLICT(role_id,permission_id) DO NOTHING;

-- Sprint 14 / V1.5: LGPD and security governance (idempotent, non-destructive)
CREATE TABLE IF NOT EXISTS orcafacil.privacy_consents (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NOT NULL, consent_type varchar(40) NOT NULL, version varchar(40) NOT NULL, accepted_at timestamptz NOT NULL, revoked_at timestamptz NULL, ip_address varchar(64) NOT NULL DEFAULT '', user_agent varchar(512) NOT NULL DEFAULT '', created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.data_export_jobs (id uuid PRIMARY KEY, account_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, scope text NOT NULL, format text NOT NULL, status text NOT NULL, requested_at timestamptz NOT NULL, completed_at timestamptz NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.data_retention_policies (id uuid PRIMARY KEY, account_id uuid NOT NULL, data_type text NOT NULL, retention_days integer NOT NULL CHECK (retention_days >= 0), action varchar(30) NOT NULL, is_active boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.data_retention_runs (id uuid PRIMARY KEY, account_id uuid NOT NULL, policy_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, is_simulation boolean NOT NULL, matched_records integer NOT NULL, affected_records integer NOT NULL, started_at timestamptz NOT NULL, completed_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.sensitive_data_access_logs (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NOT NULL, entity_type text NOT NULL, entity_id uuid NOT NULL, access_type text NOT NULL, reason text NOT NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, correlation_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.security_events (id uuid PRIMARY KEY, account_id uuid NULL, user_id uuid NULL, event_type text NOT NULL, outcome text NOT NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, correlation_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.session_records (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NOT NULL, session_hash varchar(128) NOT NULL, started_at timestamptz NOT NULL, last_seen_at timestamptz NOT NULL, expires_at timestamptz NOT NULL, revoked_at timestamptz NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.public_token_access_logs (id uuid PRIMARY KEY, account_id uuid NULL, token_type text NOT NULL, entity_id uuid NOT NULL, accessed_at timestamptz NOT NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, outcome text NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.account_security_settings (id uuid PRIMARY KEY, account_id uuid NOT NULL, session_expiration_minutes integer NOT NULL, minimum_password_length integer NOT NULL, require_password_change boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.audit_export_jobs (id uuid PRIMARY KEY, account_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, status text NOT NULL, requested_at timestamptz NOT NULL, completed_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
ALTER TABLE orcafacil.audit_logs ADD COLUMN IF NOT EXISTS summary text NOT NULL DEFAULT '';
ALTER TABLE orcafacil.audit_logs ADD COLUMN IF NOT EXISTS correlation_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE orcafacil.data_subject_requests ADD COLUMN IF NOT EXISTS client_id uuid NULL;
ALTER TABLE orcafacil.data_subject_requests ADD COLUMN IF NOT EXISTS resolution_notes text NULL;
ALTER TABLE orcafacil.data_subject_requests ADD COLUMN IF NOT EXISTS reviewed_at timestamptz NULL;
CREATE INDEX IF NOT EXISTS ix_privacy_consents_account_user ON orcafacil.privacy_consents(account_id, user_id, consent_type);
CREATE INDEX IF NOT EXISTS ix_data_export_jobs_account_created ON orcafacil.data_export_jobs(account_id, created_at);
CREATE UNIQUE INDEX IF NOT EXISTS ix_retention_policy_account_type ON orcafacil.data_retention_policies(account_id, data_type) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_sensitive_access_account_entity ON orcafacil.sensitive_data_access_logs(account_id, entity_type, entity_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_security_events_account_created ON orcafacil.security_events(account_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_sessions_account_user ON orcafacil.session_records(account_id, user_id, revoked_at);
CREATE INDEX IF NOT EXISTS ix_public_token_access_account_date ON orcafacil.public_token_access_logs(account_id, accessed_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ix_account_security_settings_account ON orcafacil.account_security_settings(account_id) WHERE is_deleted = false;
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['Privacy.View','Privacy.Manage','Privacy.ExportData','Privacy.AnonymizeData','Privacy.ManageRetention','Audit.View','Audit.Export','SensitiveData.View','Security.ViewSessions','Security.ManageSessions','Security.ManageUsers','Tokens.Revoke','Files.DownloadPrivate','Files.ManageVisibility']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted) SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE r.code IN ('Owner','Administrator') AND p.code IN ('Privacy.View','Privacy.Manage','Privacy.ExportData','Privacy.AnonymizeData','Privacy.ManageRetention','Audit.View','Audit.Export','SensitiveData.View','Security.ViewSessions','Security.ManageSessions','Security.ManageUsers','Tokens.Revoke','Files.DownloadPrivate','Files.ManageVisibility') ON CONFLICT(role_id,permission_id) DO NOTHING;
-- Sprint 15 / V1.6: operações SaaS (idempotente e preserva dados)
CREATE TABLE IF NOT EXISTS orcafacil.background_jobs (id uuid PRIMARY KEY, name varchar(160) NOT NULL, schedule varchar(80) NOT NULL, is_enabled boolean NOT NULL DEFAULT true, next_execution_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_background_jobs_name ON orcafacil.background_jobs(name);
CREATE TABLE IF NOT EXISTS orcafacil.job_executions (id uuid PRIMARY KEY, job_name varchar(160) NOT NULL, execution_id varchar(160) NOT NULL, instance_id varchar(160) NOT NULL, status varchar(24) NOT NULL, started_at timestamptz NOT NULL, finished_at timestamptz NULL, duration_milliseconds bigint NULL, error_summary varchar(500) NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_job_executions_execution_id ON orcafacil.job_executions(execution_id);
CREATE INDEX IF NOT EXISTS ix_job_executions_job_started ON orcafacil.job_executions(job_name, started_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.job_locks (id uuid PRIMARY KEY, name varchar(160) NOT NULL, locked_by varchar(160) NULL, locked_until timestamptz NULL, acquired_at timestamptz NULL, released_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_job_locks_name ON orcafacil.job_locks(name);
CREATE TABLE IF NOT EXISTS orcafacil.processing_outbox (id uuid PRIMARY KEY, account_id uuid NOT NULL, type varchar(80) NOT NULL, idempotency_key varchar(200) NOT NULL, payload_json jsonb NOT NULL DEFAULT '{}', status varchar(24) NOT NULL, priority integer NOT NULL DEFAULT 0, attempts integer NOT NULL DEFAULT 0, maximum_attempts integer NOT NULL DEFAULT 5, next_attempt_at timestamptz NOT NULL, processing_started_at timestamptz NULL, processing_instance_id varchar(160) NULL, last_error varchar(500) NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_processing_outbox_account_key ON orcafacil.processing_outbox(account_id,idempotency_key);
CREATE INDEX IF NOT EXISTS ix_processing_outbox_ready ON orcafacil.processing_outbox(status,next_attempt_at,priority DESC) WHERE is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.system_metrics (id uuid PRIMARY KEY, account_id uuid NULL, name varchar(160) NOT NULL, value double precision NOT NULL, period_start timestamptz NOT NULL, period_end timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_system_metrics_scope_period ON orcafacil.system_metrics(account_id,name,period_start DESC);
CREATE TABLE IF NOT EXISTS orcafacil.slow_query_logs (id uuid PRIMARY KEY, account_id uuid NULL, user_id uuid NULL, route varchar(300) NOT NULL, operation varchar(160) NOT NULL, elapsed_milliseconds bigint NOT NULL, correlation_id varchar(160) NOT NULL, summary varchar(500) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_slow_query_logs_scope_created ON orcafacil.slow_query_logs(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_usage_metrics (id uuid PRIMARY KEY, account_id uuid NOT NULL, resource varchar(100) NOT NULL, used bigint NOT NULL, "limit" bigint NULL, period_start timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_tenant_usage_scope_resource_period ON orcafacil.tenant_usage_metrics(account_id,resource,period_start);
CREATE TABLE IF NOT EXISTS orcafacil.cache_invalidation_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, cache_region varchar(100) NOT NULL, reason varchar(300) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_cache_invalidations_scope_created ON orcafacil.cache_invalidation_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.quota_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, resource varchar(100) NOT NULL, used bigint NOT NULL, "limit" bigint NOT NULL, blocked boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_quota_events_scope_created ON orcafacil.quota_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.rate_limit_events (id uuid PRIMARY KEY, account_id uuid NULL, policy varchar(100) NOT NULL, client_fingerprint varchar(128) NOT NULL, route varchar(300) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_rate_limit_events_policy_created ON orcafacil.rate_limit_events(policy,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.worker_heartbeats (id uuid PRIMARY KEY, instance_id varchar(160) NOT NULL, last_seen_at timestamptz NOT NULL, status varchar(40) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_worker_heartbeats_instance ON orcafacil.worker_heartbeats(instance_id);

-- Sprint 16 / V1.7: estruturas enterprise aditivas e idempotentes.
CREATE TABLE IF NOT EXISTS orcafacil.business_units (id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(160) NOT NULL, legal_name varchar(200), document_number varchar(32), email varchar(254), phone varchar(40), whats_app varchar(40), address_line varchar(300), city varchar(120), state varchar(2), zip_code varchar(16), is_main boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_business_units_main ON orcafacil.business_units(account_id) WHERE is_main AND is_active AND NOT is_deleted;
CREATE INDEX IF NOT EXISTS ix_business_units_account ON orcafacil.business_units(account_id, is_active);
CREATE TABLE IF NOT EXISTS orcafacil.business_unit_members (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid NOT NULL, user_id uuid NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_business_unit_members ON orcafacil.business_unit_members(account_id,business_unit_id,user_id);
CREATE TABLE IF NOT EXISTS orcafacil.teams (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid, name varchar(160) NOT NULL, description text, type varchar(24) NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_teams_name ON orcafacil.teams(account_id,name);
CREATE TABLE IF NOT EXISTS orcafacil.team_members (id uuid PRIMARY KEY, account_id uuid NOT NULL, team_id uuid NOT NULL, user_id uuid NOT NULL, role_in_team varchar(100), is_leader boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_team_members ON orcafacil.team_members(account_id,team_id,user_id);
CREATE TABLE IF NOT EXISTS orcafacil.role_profiles (id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(100) NOT NULL, description text, is_system boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_role_profiles_name ON orcafacil.role_profiles(account_id,name);
CREATE TABLE IF NOT EXISTS orcafacil.role_profile_permissions (id uuid PRIMARY KEY, account_id uuid NOT NULL, role_profile_id uuid NOT NULL, permission_code varchar(120) NOT NULL, is_enabled boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_role_profile_permissions ON orcafacil.role_profile_permissions(account_id,role_profile_id,permission_code);
CREATE TABLE IF NOT EXISTS orcafacil.discount_policies (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid, name varchar(160) NOT NULL, max_discount_percent_without_approval numeric(18,2) NOT NULL DEFAULT 0, max_discount_amount_without_approval numeric(18,2) NOT NULL DEFAULT 0, requires_approval_above_amount numeric(18,2), require_different_approver boolean NOT NULL DEFAULT true, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_discount_policies_scope ON orcafacil.discount_policies(account_id,business_unit_id,is_active);
CREATE TABLE IF NOT EXISTS orcafacil.approval_requests (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, approver_user_id uuid, status varchar(24) NOT NULL, reason varchar(1000) NOT NULL, requested_at timestamptz NOT NULL DEFAULT now(), decided_at timestamptz, require_different_approver boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_approval_queue ON orcafacil.approval_requests(account_id,status,approver_user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_approval_pending_document ON orcafacil.approval_requests(account_id,document_id) WHERE status='Pending' AND NOT is_deleted;
CREATE TABLE IF NOT EXISTS orcafacil.approval_request_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, approval_request_id uuid NOT NULL, actor_user_id uuid NOT NULL, type varchar(24) NOT NULL, comment varchar(2000), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_approval_events ON orcafacil.approval_request_events(account_id,approval_request_id,created_at);
CREATE TABLE IF NOT EXISTS orcafacil.white_label_settings (id uuid PRIMARY KEY, account_id uuid NOT NULL, display_name varchar(160), logo_path varchar(500), primary_color varchar(16) NOT NULL, secondary_color varchar(16) NOT NULL, footer_text varchar(500), remove_orca_facil_brand boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_white_label_account ON orcafacil.white_label_settings(account_id);
CREATE TABLE IF NOT EXISTS orcafacil.unit_branding_profiles (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid NOT NULL, trade_name varchar(160), logo_path varchar(500), document_logo_path varchar(500), primary_color varchar(16), secondary_color varchar(16), footer_text varchar(500), email_text text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_unit_branding ON orcafacil.unit_branding_profiles(account_id,business_unit_id);
CREATE TABLE IF NOT EXISTS orcafacil.document_visibility_rules (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid, team_id uuid, user_id uuid, restrict_to_assignments boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_document_visibility_scope ON orcafacil.document_visibility_rules(account_id,business_unit_id,team_id,user_id);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS business_unit_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_to_user_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_team_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
CREATE INDEX IF NOT EXISTS ix_documents_enterprise_scope ON orcafacil.documents(account_id,business_unit_id,assigned_team_id,assigned_to_user_id);

-- Sprint 19: safe V2 productivity permissions. This patch is repeatable and preserves existing rows.
INSERT INTO orcafacil.permissions(code, display_name, is_platform_permission)
SELECT code, code, false FROM unnest(ARRAY[
  'Search.Global','CommandCenter.Use','Assistant.Use','KnowledgeBase.View','GuidedTours.View',
  'GuidedTours.Manage','Onboarding.Manage','Productivity.View','Activity.View','Shortcuts.ManageOwn','Favorites.ManageOwn'
]) AS code ON CONFLICT (code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id, permission_id, created_at, is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE r.code IN ('Owner','Administrator','Collaborator','Viewer')
  AND p.code IN ('Search.Global','CommandCenter.Use','Assistant.Use','KnowledgeBase.View','GuidedTours.View','Productivity.View','Activity.View','Shortcuts.ManageOwn','Favorites.ManageOwn')
ON CONFLICT (role_id,permission_id) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id, permission_id, created_at, is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE r.code IN ('Owner','Administrator') AND p.code IN ('GuidedTours.Manage','Onboarding.Manage')
ON CONFLICT (role_id,permission_id) DO NOTHING;
