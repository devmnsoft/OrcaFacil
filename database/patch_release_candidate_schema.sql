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
