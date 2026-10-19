-- ORCAFACIL / FECHO 2.0 - IMPORTACAO UNICA PARA POSTGRESQL
-- Consolida carga inicial + todos os hotfixes. Idempotente, sem DROP/TRUNCATE.
-- Pre-requisito: migracoes EF aplicadas (schema existente). ON_ERROR_STOP=1.
-- Ordem:
--   1. script_completop.sql
--   2. hotfix_ai_apply_governance_v69.sql
--   3. hotfix_ai_budget_assistant_v67.sql
--   4. hotfix_ai_governance_permissions_v74.sql
--   5. hotfix_ai_governance_permissions_v76.sql
--   6. hotfix_ai_quota_reservation_v68.sql
--   7. hotfix_billing_coverage_v67.sql
--   8. hotfix_budget_templates_commercial_fields_v72.sql
--   9. hotfix_client_rowversion_v70.sql
--   10. hotfix_commercial_schema_drift_v57.sql
--   11. hotfix_document_sequences_status_compat_v73.sql
--   12. hotfix_document_sequences_v71.sql
--   13. hotfix_documents_commercial_columns.sql
--   14. hotfix_documents_full_schema_drift_v61.sql
--   15. hotfix_documents_row_version_schema_drift_v60.sql
--   16. hotfix_plan_feature_catalog_v75.sql
--   17. hotfix_quote_workspace_balance_v73.sql
--   18. hotfix_release_candidate_schema_v64.sql
--   19. hotfix_saas_enterprise_modules_v66.sql
--   20. hotfix_schema_drift_quality_gate_v62.sql


-- >>> script_completop.sql
-- =========================================================
-- 1. Cabeçalho
-- OrçaFácil - Script completo PostgreSQL
-- Schema único da aplicação: orcafacil
-- Seguro para reexecução: CREATE IF NOT EXISTS, CREATE INDEX IF NOT EXISTS
-- e INSERT ... ON CONFLICT. Não cria usuários administrativos com senha fixa.
-- =========================================================

BEGIN;

-- 2. Extensões
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 3. Schema
CREATE SCHEMA IF NOT EXISTS orcafacil;

-- 4. Tabelas de usuários
CREATE TABLE IF NOT EXISTS orcafacil.users (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    name varchar(180) NOT NULL,
    email varchar(254) NOT NULL,
    phone varchar(40),
    password_hash varchar(500) NOT NULL,
    role varchar(40) NOT NULL DEFAULT 'User',
    plan varchar(40) NOT NULL DEFAULT 'Free',
    is_active boolean NOT NULL DEFAULT true,
    is_blocked boolean NOT NULL DEFAULT false,
    block_reason varchar(500),
    accepted_terms_at timestamptz,
    accepted_privacy_at timestamptz,
    last_login_at timestamptz,
    last_seen_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_users PRIMARY KEY (id),
    CONSTRAINT uq_orcafacil_users_email UNIQUE (email),
    CONSTRAINT ck_orcafacil_users_role CHECK (role IN ('User', 'Admin', 'SuperAdmin')),
    CONSTRAINT ck_orcafacil_users_plan CHECK (plan IN ('Free', 'Pro', 'Professional', 'Business', 'Enterprise'))
);

-- Contrato de autenticação (também corrige instalações em que users já existia).
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_privacy_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_terms_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS block_reason varchar(500) NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS failed_login_attempts integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS is_blocked boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_failed_login_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_successful_login_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS legacy_unversioned_acceptance boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS locked_until timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_by_user_id uuid NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_expires_at timestamptz NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_reset_reason varchar(500) NULL;
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;

CREATE TABLE IF NOT EXISTS orcafacil.issuer_profiles (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    business_name varchar(180) NOT NULL,
    document_number varchar(32),
    phone varchar(40),
    email varchar(254),
    address varchar(300),
    city varchar(120),
    pix_key varchar(180),
    logo_path varchar(500),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_issuer_profiles PRIMARY KEY (id),
    CONSTRAINT uq_orcafacil_issuer_profiles_user UNIQUE (user_id),
    CONSTRAINT fk_orcafacil_issuer_profiles_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE
);

-- 5. Tabelas de documentos
CREATE TABLE IF NOT EXISTS orcafacil.documents (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    type varchar(40) NOT NULL,
    number varchar(40) NOT NULL,
    status varchar(40) NOT NULL,
    client_name varchar(180) NOT NULL,
    client_document varchar(32),
    client_phone varchar(40),
    client_email varchar(254),
    client_city varchar(120),
    issue_date timestamptz NOT NULL DEFAULT now(),
    valid_until timestamptz,
    payment_method varchar(60),
    notes varchar(4000),
    subtotal numeric(18,2) NOT NULL DEFAULT 0,
    discount numeric(18,2) NOT NULL DEFAULT 0,
    total numeric(18,2) NOT NULL DEFAULT 0,
    public_token varchar(128),
    public_enabled boolean NOT NULL DEFAULT false,
    client_decision varchar(40) NOT NULL DEFAULT 'Pending',
    client_decision_at timestamptz,
    client_decision_note varchar(1000),
    evidence_hash varchar(128),
    origin_budget_id uuid,
    origin_budget_number varchar(40),
    converted_receipt_id uuid,
    converted_receipt_number varchar(40),
    is_deleted boolean NOT NULL DEFAULT false,
    deleted_at timestamptz,
    deleted_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    CONSTRAINT pk_orcafacil_documents PRIMARY KEY (id),
    CONSTRAINT uq_orcafacil_documents_user_type_number UNIQUE (user_id, type, number),
    CONSTRAINT fk_orcafacil_documents_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE,
    CONSTRAINT ck_orcafacil_documents_type CHECK (type IN ('Budget', 'Receipt')),
    CONSTRAINT ck_orcafacil_documents_client_decision CHECK (client_decision IN ('Pending', 'Approved', 'Rejected'))
);

CREATE TABLE IF NOT EXISTS orcafacil.document_items (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    document_id uuid NOT NULL,
    description varchar(500) NOT NULL,
    quantity numeric(18,4) NOT NULL DEFAULT 1,
    unit_price numeric(18,2) NOT NULL DEFAULT 0,
    discount numeric(18,2) NOT NULL DEFAULT 0,
    total numeric(18,2) NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_document_items PRIMARY KEY (id),
    CONSTRAINT fk_orcafacil_document_items_documents FOREIGN KEY (document_id) REFERENCES orcafacil.documents(id) ON DELETE CASCADE
);

-- 6. Tabelas públicas
CREATE TABLE IF NOT EXISTS orcafacil.public_quotes (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    token varchar(128) NOT NULL,
    owner_user_id uuid NOT NULL,
    document_id uuid NOT NULL,
    public_enabled boolean NOT NULL DEFAULT true,
    expires_at timestamptz,
    view_count integer NOT NULL DEFAULT 0,
    last_access_at timestamptz,
    decision_status varchar(40) NOT NULL DEFAULT 'Pending',
    decision_note varchar(1000),
    decided_at timestamptz,
    decided_by_name varchar(180),
    decided_by_document varchar(32),
    decided_by_email varchar(254),
    accepted_terms boolean NOT NULL DEFAULT false,
    evidence_hash varchar(128),
    user_agent varchar(1000),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_public_quotes PRIMARY KEY (id),
    CONSTRAINT uq_orcafacil_public_quotes_token UNIQUE (token),
    CONSTRAINT fk_orcafacil_public_quotes_users FOREIGN KEY (owner_user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_orcafacil_public_quotes_documents FOREIGN KEY (document_id) REFERENCES orcafacil.documents(id) ON DELETE CASCADE,
    CONSTRAINT ck_orcafacil_public_quotes_decision CHECK (decision_status IN ('Pending', 'Approved', 'Rejected'))
);

-- 7. Tabelas de planos/cobrança
CREATE TABLE IF NOT EXISTS orcafacil.user_usage (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    period varchar(7) NOT NULL,
    documents_created integer NOT NULL DEFAULT 0,
    budgets_created integer NOT NULL DEFAULT 0,
    receipts_created integer NOT NULL DEFAULT 0,
    pdf_generated integer NOT NULL DEFAULT 0,
    public_links_created integer NOT NULL DEFAULT 0,
    backup_exports integer NOT NULL DEFAULT 0,
    chatbot_questions integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_user_usage PRIMARY KEY (id),
    CONSTRAINT uq_orcafacil_user_usage_user_period UNIQUE (user_id, period),
    CONSTRAINT fk_orcafacil_user_usage_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS orcafacil.subscriptions (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    provider varchar(40) NOT NULL DEFAULT 'Manual',
    status varchar(40) NOT NULL DEFAULT 'None',
    plan varchar(40) NOT NULL DEFAULT 'Free',
    billing_cycle varchar(40),
    amount numeric(18,2) NOT NULL DEFAULT 0,
    started_at timestamptz,
    expires_at timestamptz,
    cancelled_at timestamptz,
    last_payment_at timestamptz,
    external_customer_id varchar(180),
    external_subscription_id varchar(180),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_subscriptions PRIMARY KEY (id),
    CONSTRAINT fk_orcafacil_subscriptions_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS orcafacil.payments (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    provider varchar(40) NOT NULL DEFAULT 'Manual',
    status varchar(40) NOT NULL DEFAULT 'Pending',
    plan varchar(40) NOT NULL DEFAULT 'Free',
    billing_cycle varchar(40),
    amount numeric(18,2) NOT NULL DEFAULT 0,
    currency varchar(10) NOT NULL DEFAULT 'BRL',
    external_payment_id varchar(180),
    external_preference_id varchar(180),
    payment_method varchar(80),
    payer_email varchar(254),
    approved_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_payments PRIMARY KEY (id),
    CONSTRAINT fk_orcafacil_payments_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE
);

-- 8. Tabelas administrativas
CREATE TABLE IF NOT EXISTS orcafacil.admin_settings (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    key varchar(120) NOT NULL,
    value_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    updated_by uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_admin_settings PRIMARY KEY (id),
    CONSTRAINT uq_orcafacil_admin_settings_key UNIQUE (key)
);

CREATE TABLE IF NOT EXISTS orcafacil.notifications (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    title varchar(180) NOT NULL,
    message varchar(1000) NOT NULL,
    type varchar(60) NOT NULL DEFAULT 'Info',
    read boolean NOT NULL DEFAULT false,
    document_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_notifications PRIMARY KEY (id),
    CONSTRAINT fk_orcafacil_notifications_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE
);

-- 9. Tabelas de logs/auditoria
CREATE TABLE IF NOT EXISTS orcafacil.audit_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid,
    action varchar(120) NOT NULL,
    entity_type varchar(120) NOT NULL,
    entity_id varchar(120),
    before_json jsonb,
    after_json jsonb,
    metadata_json jsonb,
    ip_address varchar(80),
    user_agent varchar(1000),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_audit_logs PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS orcafacil.system_logs (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    level varchar(40) NOT NULL DEFAULT 'Info',
    type varchar(80) NOT NULL DEFAULT 'Application',
    message text NOT NULL,
    user_id uuid,
    user_email varchar(254),
    metadata_json jsonb,
    error_message text,
    error_stack text,
    environment varchar(80),
    url varchar(1000),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_system_logs PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS orcafacil.system_errors (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    message text NOT NULL,
    stack text,
    code varchar(80),
    severity varchar(40) NOT NULL DEFAULT 'Error',
    user_id uuid,
    context_json jsonb,
    resolved boolean NOT NULL DEFAULT false,
    resolved_at timestamptz,
    resolved_by uuid,
    admin_note varchar(1000),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_system_errors PRIMARY KEY (id)
);

-- 10. Índices
CREATE INDEX IF NOT EXISTS ix_orcafacil_users_email ON orcafacil.users(email);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_user_id ON orcafacil.documents(user_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_user_type ON orcafacil.documents(user_id, type);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_user_status ON orcafacil.documents(user_id, status);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_client_name ON orcafacil.documents(client_name);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_created_at ON orcafacil.documents(created_at);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_public_token ON orcafacil.documents(public_token);
CREATE INDEX IF NOT EXISTS ix_orcafacil_document_items_document_id ON orcafacil.document_items(document_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_public_quotes_token ON orcafacil.public_quotes(token);
CREATE INDEX IF NOT EXISTS ix_orcafacil_public_quotes_document_id ON orcafacil.public_quotes(document_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_user_usage_user_period ON orcafacil.user_usage(user_id, period);
CREATE INDEX IF NOT EXISTS ix_orcafacil_audit_logs_created_at ON orcafacil.audit_logs(created_at);
CREATE INDEX IF NOT EXISTS ix_orcafacil_system_logs_created_at ON orcafacil.system_logs(created_at);
CREATE INDEX IF NOT EXISTS ix_orcafacil_system_errors_created_at ON orcafacil.system_errors(created_at);
CREATE INDEX IF NOT EXISTS ix_orcafacil_system_errors_resolved ON orcafacil.system_errors(resolved);
CREATE INDEX IF NOT EXISTS ix_orcafacil_notifications_user_id ON orcafacil.notifications(user_id);

-- 11. Seeds
INSERT INTO orcafacil.admin_settings (key, value_json, updated_at) VALUES
('company', '{"companyName":"MNSOFT","companyCnpj":"18.160.057/0001-13","companyEmail":"comercial@mnsoft.com.br","supportEmail":"comercial@mnsoft.com.br"}'::jsonb, now()),
('contact', '{"whatsappEnabled":true,"whatsappNumber":"5591981809035","whatsappDisplayName":"Atendimento MNSOFT","whatsappDefaultMessage":"Olá, gostaria de falar com a MNSOFT sobre o OrçaFácil.","emailEnabled":true,"email":"comercial@mnsoft.com.br"}'::jsonb, now()),
('plans', '{"free":{"name":"Free","priceMonthly":0,"maxDocumentsPerMonth":20,"maxPdfPerMonth":20,"watermark":true},"pro":{"name":"Pro","priceMonthly":19.90,"priceYearly":199.00,"maxDocumentsPerMonth":null,"maxPdfPerMonth":null,"watermark":false}}'::jsonb, now()),
('logging', '{"level":"Information","retentionDays":30}'::jsonb, now()),
('security', '{"cookieAuth":true,"rateLimit":true}'::jsonb, now()),
('theme', '{"primary":"#0d6efd"}'::jsonb, now()),
('terms', '{"version":"1.0","required":true}'::jsonb, now())
ON CONFLICT (key) DO UPDATE
SET value_json = EXCLUDED.value_json,
    updated_at = now();

-- SaaS Admin/Billing evolution (safe re-run)
CREATE TABLE IF NOT EXISTS orcafacil.clients (
    id uuid NOT NULL DEFAULT gen_random_uuid(), user_id uuid NOT NULL,
    person_type varchar(30) NOT NULL DEFAULT 'Individual', document_type varchar(10), document_number varchar(20),
    name varchar(180) NOT NULL, trade_name varchar(180), legal_name varchar(180), email varchar(254), phone varchar(40), city varchar(120), address varchar(300), notes varchar(1000),
    created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_clients PRIMARY KEY (id), CONSTRAINT fk_orcafacil_clients_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE,
    CONSTRAINT ck_orcafacil_clients_person_type CHECK (person_type IN ('Individual', 'Company')),
    CONSTRAINT ck_orcafacil_clients_document_type CHECK (document_type IS NULL OR document_type IN ('CPF', 'CNPJ'))
);
CREATE INDEX IF NOT EXISTS ix_orcafacil_clients_user_id ON orcafacil.clients(user_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_clients_name ON orcafacil.clients(name);
CREATE INDEX IF NOT EXISTS ix_orcafacil_clients_document_number ON orcafacil.clients(document_number);

CREATE TABLE IF NOT EXISTS orcafacil.billing_customer_profiles (
    id uuid NOT NULL DEFAULT gen_random_uuid(), user_id uuid NOT NULL,
    person_type varchar(30) NOT NULL DEFAULT 'Individual', document_type varchar(10), document_number varchar(20), name varchar(180) NOT NULL, trade_name varchar(180), legal_name varchar(180), email varchar(254), phone varchar(40), city varchar(120), address varchar(300), mercado_pago_customer_id varchar(180),
    created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT pk_orcafacil_billing_customer_profiles PRIMARY KEY (id), CONSTRAINT uq_orcafacil_billing_customer_profiles_user UNIQUE (user_id), CONSTRAINT fk_orcafacil_billing_customer_profiles_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE,
    CONSTRAINT ck_orcafacil_billing_profiles_person_type CHECK (person_type IN ('Individual', 'Company')), CONSTRAINT ck_orcafacil_billing_profiles_document_type CHECK (document_type IS NULL OR document_type IN ('CPF', 'CNPJ'))
);

CREATE TABLE IF NOT EXISTS orcafacil.plan_features (id uuid NOT NULL DEFAULT gen_random_uuid(), plan_code varchar(40) NOT NULL, feature_code varchar(120) NOT NULL, is_enabled boolean NOT NULL DEFAULT false, limit_value integer, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_plan_features PRIMARY KEY (id), CONSTRAINT uq_orcafacil_plan_features UNIQUE(plan_code, feature_code));
CREATE TABLE IF NOT EXISTS orcafacil.payment_events (id uuid NOT NULL DEFAULT gen_random_uuid(), payment_id uuid NOT NULL, action varchar(120) NOT NULL, status varchar(40) NOT NULL, raw_json text, correlation_id varchar(120), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_payment_events PRIMARY KEY(id));
CREATE TABLE IF NOT EXISTS orcafacil.mercadopago_webhook_events (id uuid NOT NULL DEFAULT gen_random_uuid(), event_key varchar(180) NOT NULL, external_payment_id varchar(180), topic varchar(80), raw_json text NOT NULL DEFAULT '{}', processed boolean NOT NULL DEFAULT false, correlation_id varchar(120), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_mercadopago_webhook_events PRIMARY KEY(id), CONSTRAINT uq_orcafacil_mercadopago_webhook_events_key UNIQUE(event_key));

ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS grace_until timestamptz;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS subscription_id uuid;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS due_date timestamptz;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS paid_at timestamptz;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS expires_at timestamptz;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS external_reference varchar(180);
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS pix_qr_code text;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS pix_qr_code_base64 text;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS pix_ticket_url text;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS boleto_url text;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS boleto_barcode text;
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS idempotency_key varchar(180);
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS raw_response_json text;
CREATE INDEX IF NOT EXISTS ix_orcafacil_payments_user_id ON orcafacil.payments(user_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_payments_subscription_id ON orcafacil.payments(subscription_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_payments_status ON orcafacil.payments(status);
CREATE INDEX IF NOT EXISTS ix_orcafacil_payments_provider ON orcafacil.payments(provider);
CREATE INDEX IF NOT EXISTS ix_orcafacil_payments_external_payment_id ON orcafacil.payments(external_payment_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_payments_external_reference ON orcafacil.payments(external_reference);
CREATE INDEX IF NOT EXISTS ix_orcafacil_payments_due_date ON orcafacil.payments(due_date);


-- 12. Validação opcional
DO $$
DECLARE
    missing_count integer;
BEGIN
    SELECT count(*) INTO missing_count
      FROM unnest(ARRAY[
        'users','issuer_profiles','documents','document_items','public_quotes',
        'user_usage','subscriptions','payments','payment_events','mercadopago_webhook_events',
        'billing_customer_profiles','clients','plan_features','admin_settings','notifications',
        'audit_logs','system_logs','system_errors'
      ]) AS required(table_name)
     WHERE to_regclass('orcafacil.' || required.table_name) IS NULL;

    IF missing_count > 0 THEN
        RAISE NOTICE 'Schema orcafacil incompleto: % tabelas obrigatórias ausentes.', missing_count;
    END IF;
END $$;


-- Modelos de orçamento por profissão (idempotente)
CREATE TABLE IF NOT EXISTS orcafacil.budget_templates (
    id uuid PRIMARY KEY,
    account_id uuid NULL,
    user_id uuid NULL,
    profession varchar(80) NOT NULL,
    title varchar(160) NOT NULL,
    description varchar(800) NOT NULL,
    is_system_template boolean NOT NULL DEFAULT true,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NULL,
    is_deleted boolean NOT NULL DEFAULT false
);

CREATE TABLE IF NOT EXISTS orcafacil.budget_template_items (
    id uuid PRIMARY KEY,
    budget_template_id uuid NOT NULL REFERENCES orcafacil.budget_templates(id) ON DELETE CASCADE,
    description varchar(300) NOT NULL,
    quantity numeric(18,2) NOT NULL DEFAULT 1,
    unit_price numeric(18,2) NOT NULL DEFAULT 0,
    unit varchar(30) NOT NULL DEFAULT 'un',
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NULL,
    is_deleted boolean NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_orcafacil_budget_templates_system_profession ON orcafacil.budget_templates(profession) WHERE is_system_template = true;
CREATE INDEX IF NOT EXISTS ix_orcafacil_budget_templates_profession ON orcafacil.budget_templates(profession);
CREATE INDEX IF NOT EXISTS ix_orcafacil_budget_templates_user_id ON orcafacil.budget_templates(user_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_budget_template_items_template_id ON orcafacil.budget_template_items(budget_template_id);

INSERT INTO orcafacil.budget_templates (id, profession, title, description, is_system_template, is_active)
VALUES
('11111111-1111-1111-1111-111111111111','Eletricista','Modelo de orçamento para eletricista','Instalação de tomada, troca de disjuntor e revisão elétrica.' ,true,true),
('22222222-2222-2222-2222-222222222222','Pintor','Modelo de orçamento para pintor','Pintura de parede, massa corrida e acabamento.' ,true,true),
('33333333-3333-3333-3333-333333333333','Pedreiro','Modelo de orçamento para pedreiro','Reforma, assentamento e reparos.' ,true,true),
('44444444-4444-4444-4444-444444444444','Técnico','Modelo de orçamento para técnico','Visita técnica, diagnóstico e manutenção.' ,true,true),
('55555555-5555-5555-5555-555555555555','Designer','Modelo de orçamento para designer','Arte para redes sociais, logotipo e identidade visual.' ,true,true),
('66666666-6666-6666-6666-666666666666','Fotógrafo','Modelo de orçamento para fotógrafo','Ensaio, edição e entrega digital.' ,true,true),
('77777777-7777-7777-7777-777777777777','Diarista','Modelo de orçamento para diarista','Limpeza comum, limpeza pesada e organização.' ,true,true),
('88888888-8888-8888-8888-888888888888','Beleza/Manicure','Modelo de orçamento para beleza/manicure','Atendimento, acabamento e cuidados extras.' ,true,true)
ON CONFLICT (id) DO UPDATE SET title = EXCLUDED.title, description = EXCLUDED.description, is_active = true;

INSERT INTO orcafacil.budget_template_items (id, budget_template_id, description, quantity, unit_price, unit, sort_order)
VALUES
('11111111-0000-0000-0000-000000000001','11111111-1111-1111-1111-111111111111','Instalação de tomada',1,120,'un',1),('11111111-0000-0000-0000-000000000002','11111111-1111-1111-1111-111111111111','Troca de disjuntor',1,180,'un',2),('11111111-0000-0000-0000-000000000003','11111111-1111-1111-1111-111111111111','Revisão elétrica',1,350,'serviço',3),
('22222222-0000-0000-0000-000000000001','22222222-2222-2222-2222-222222222222','Pintura de parede',1,520,'serviço',1),('22222222-0000-0000-0000-000000000002','22222222-2222-2222-2222-222222222222','Massa corrida',1,280,'serviço',2),('22222222-0000-0000-0000-000000000003','22222222-2222-2222-2222-222222222222','Acabamento',1,140,'serviço',3),
('33333333-0000-0000-0000-000000000001','33333333-3333-3333-3333-333333333333','Reforma',1,900,'serviço',1),('33333333-0000-0000-0000-000000000002','33333333-3333-3333-3333-333333333333','Assentamento',1,650,'serviço',2),('33333333-0000-0000-0000-000000000003','33333333-3333-3333-3333-333333333333','Reparos',1,240,'serviço',3),
('44444444-0000-0000-0000-000000000001','44444444-4444-4444-4444-444444444444','Visita técnica',1,100,'un',1),('55555555-0000-0000-0000-000000000001','55555555-5555-5555-5555-555555555555','Arte para redes sociais',1,250,'un',1),('55555555-0000-0000-0000-000000000002','55555555-5555-5555-5555-555555555555','Logotipo',1,700,'un',2),('55555555-0000-0000-0000-000000000003','55555555-5555-5555-5555-555555555555','Identidade visual',1,1200,'projeto',3),
('66666666-0000-0000-0000-000000000001','66666666-6666-6666-6666-666666666666','Ensaio fotográfico',1,450,'serviço',1),('77777777-0000-0000-0000-000000000001','77777777-7777-7777-7777-777777777777','Diária de limpeza',1,180,'diária',1),('88888888-0000-0000-0000-000000000001','88888888-8888-8888-8888-888888888888','Manicure/beleza',1,80,'serviço',1)
ON CONFLICT (id) DO UPDATE SET description = EXCLUDED.description, quantity = EXCLUDED.quantity, unit_price = EXCLUDED.unit_price, unit = EXCLUDED.unit, sort_order = EXCLUDED.sort_order;

-- 18. Contas, permissões e catálogo comercial (evolução aditiva e idempotente)
CREATE TABLE IF NOT EXISTS orcafacil.business_accounts (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), display_name varchar(180) NOT NULL, legal_name varchar(180), trade_name varchar(180), person_type varchar(30) NOT NULL DEFAULT 'Individual', document_type varchar(20), document_number varchar(20), email varchar(254) NOT NULL, phone varchar(40), status varchar(30) NOT NULL DEFAULT 'Active', current_plan_code varchar(40) NOT NULL DEFAULT 'FREE', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, activated_at timestamptz, deactivated_at timestamptz, blocked_at timestamptz, block_reason varchar(500), is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS uq_business_accounts_document_number ON orcafacil.business_accounts(document_number) WHERE is_deleted = false;
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS account_id uuid REFERENCES orcafacil.business_accounts(id);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS state varchar(2);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS postal_code varchar(8);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS street varchar(180);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS street_number varchar(30);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS complement varchar(120);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS district varchar(120);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS address varchar(300);
ALTER TABLE orcafacil.billing_customer_profiles ADD COLUMN IF NOT EXISTS mercado_pago_customer_id varchar(180);
CREATE UNIQUE INDEX IF NOT EXISTS uq_billing_profiles_account_id ON orcafacil.billing_customer_profiles(account_id) WHERE account_id IS NOT NULL AND is_deleted = false;
CREATE UNIQUE INDEX IF NOT EXISTS uq_billing_profiles_document_number ON orcafacil.billing_customer_profiles(document_number) WHERE document_number IS NOT NULL AND is_deleted = false;
CREATE TABLE IF NOT EXISTS orcafacil.roles (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(80) NOT NULL UNIQUE, display_name varchar(120) NOT NULL, is_platform_role boolean NOT NULL DEFAULT false, is_system boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.permissions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(120) NOT NULL UNIQUE, display_name varchar(180) NOT NULL, is_platform_permission boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.role_permissions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), role_id uuid NOT NULL REFERENCES orcafacil.roles(id), permission_id uuid NOT NULL REFERENCES orcafacil.permissions(id), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(role_id, permission_id));
CREATE TABLE IF NOT EXISTS orcafacil.account_members (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), user_id uuid NOT NULL REFERENCES orcafacil.users(id), role_code varchar(80) NOT NULL, status varchar(30) NOT NULL DEFAULT 'Invited', invited_at timestamptz, joined_at timestamptz, disabled_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(account_id,user_id));
CREATE TABLE IF NOT EXISTS orcafacil.plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(40) NOT NULL UNIQUE, display_name varchar(100) NOT NULL, short_description varchar(300) NOT NULL, is_free boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, is_public boolean NOT NULL DEFAULT true, is_recommended boolean NOT NULL DEFAULT false, display_order integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.plan_versions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL REFERENCES orcafacil.plans(id), version_number integer NOT NULL, monthly_price numeric(12,2) NOT NULL, annual_price numeric(12,2) NOT NULL, currency char(3) NOT NULL DEFAULT 'BRL', trial_days integer NOT NULL DEFAULT 0, grace_period_days integer NOT NULL DEFAULT 0, valid_from timestamptz NOT NULL, valid_until timestamptz, status varchar(30) NOT NULL DEFAULT 'Draft', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, published_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(plan_id,version_number));
CREATE TABLE IF NOT EXISTS orcafacil.features (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(120) NOT NULL UNIQUE, display_name varchar(180) NOT NULL, description varchar(500) NOT NULL, value_type varchar(30) NOT NULL, category varchar(80) NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.plan_feature_values (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_version_id uuid NOT NULL REFERENCES orcafacil.plan_versions(id), feature_id uuid NOT NULL REFERENCES orcafacil.features(id), boolean_value boolean, integer_value integer, decimal_value numeric(12,2), text_value text, is_unlimited boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(plan_version_id,feature_id));
CREATE TABLE IF NOT EXISTS orcafacil.billing_invoices (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), subscription_id uuid NOT NULL REFERENCES orcafacil.subscriptions(id), plan_version_id uuid NOT NULL REFERENCES orcafacil.plan_versions(id), cycle varchar(20) NOT NULL, amount numeric(12,2) NOT NULL, currency char(3) NOT NULL DEFAULT 'BRL', due_at timestamptz NOT NULL, status varchar(30) NOT NULL, external_reference varchar(160) NOT NULL UNIQUE, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, paid_at timestamptz, cancelled_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.plan_overrides (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), plan_version_id uuid NOT NULL REFERENCES orcafacil.plan_versions(id), reason varchar(500) NOT NULL, starts_at timestamptz NOT NULL, ends_at timestamptz NOT NULL, granted_by_user_id uuid NOT NULL REFERENCES orcafacil.users(id), revoked_at timestamptz, revoked_by_user_id uuid REFERENCES orcafacil.users(id), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CHECK(ends_at > starts_at));
CREATE TABLE IF NOT EXISTS orcafacil.subscription_events (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), subscription_id uuid NOT NULL REFERENCES orcafacil.subscriptions(id), event_type varchar(80) NOT NULL, details text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.support_access_sessions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), platform_user_id uuid NOT NULL REFERENCES orcafacil.users(id), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), reason varchar(500) NOT NULL, mode varchar(20) NOT NULL DEFAULT 'ReadOnly', started_at timestamptz NOT NULL, expires_at timestamptz NOT NULL, ended_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CHECK(expires_at <= started_at + interval '30 minutes'));
CREATE TABLE IF NOT EXISTS orcafacil.activity_events (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid REFERENCES orcafacil.business_accounts(id), actor_user_id uuid REFERENCES orcafacil.users(id), action varchar(100) NOT NULL, entity_type varchar(100), entity_id uuid, result varchar(40) NOT NULL, summary varchar(500), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
DO $$ BEGIN
  ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS account_id uuid;
  ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS account_id uuid;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS account_id uuid;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS selected_plan_version_id uuid;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS effective_plan_version_id uuid;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS price_at_activation numeric(18,2) NOT NULL DEFAULT 0;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS paid_through_at timestamptz;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS next_due_at timestamptz;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS past_due_since timestamptz;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS suspended_at timestamptz;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS manual_release_until timestamptz;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_started_at timestamptz;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_ends_at timestamptz;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_used boolean NOT NULL DEFAULT false;
  ALTER TABLE orcafacil.subscriptions ADD COLUMN IF NOT EXISTS trial_status varchar(30) NOT NULL DEFAULT 'NotStarted';
  ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS account_id uuid;
  ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS account_id uuid;
  ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS category varchar(30) NOT NULL DEFAULT 'System';
  ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS action_url varchar(400);
  ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS action_text varchar(80);
  ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS read_at timestamptz;
  ALTER TABLE orcafacil.notifications ADD COLUMN IF NOT EXISTS is_read boolean NOT NULL DEFAULT false;
  ALTER TABLE orcafacil.audit_logs ADD COLUMN IF NOT EXISTS account_id uuid;
END $$;
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_subscriptions_selected_plan_version') THEN
    ALTER TABLE orcafacil.subscriptions ADD CONSTRAINT fk_subscriptions_selected_plan_version FOREIGN KEY (selected_plan_version_id) REFERENCES orcafacil.plan_versions(id);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_subscriptions_effective_plan_version') THEN
    ALTER TABLE orcafacil.subscriptions ADD CONSTRAINT fk_subscriptions_effective_plan_version FOREIGN KEY (effective_plan_version_id) REFERENCES orcafacil.plan_versions(id);
  END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_account_members_account_id ON orcafacil.account_members(account_id);
CREATE INDEX IF NOT EXISTS ix_account_members_user_id ON orcafacil.account_members(user_id);
CREATE INDEX IF NOT EXISTS ix_billing_invoices_account_id ON orcafacil.billing_invoices(account_id);
CREATE INDEX IF NOT EXISTS ix_billing_invoices_status_due_at ON orcafacil.billing_invoices(status,due_at);
CREATE INDEX IF NOT EXISTS ix_activity_events_account_created ON orcafacil.activity_events(account_id,created_at);
INSERT INTO orcafacil.plans (code,display_name,short_description,is_free,is_recommended,display_order) VALUES ('FREE','Grátis','Para começar a organizar seus documentos.',true,false,1),('PROFESSIONAL','Profissional','Para apresentar seu trabalho com mais profissionalismo.',false,true,2),('BUSINESS','Negócio','Para acompanhar clientes e trabalhar em equipe.',false,false,3),('ENTERPRISE','Enterprise','Para operações com governança, escala e atendimento comercial dedicado.',false,false,4) ON CONFLICT(code) DO UPDATE SET display_name=excluded.display_name,short_description=excluded.short_description;
INSERT INTO orcafacil.roles(code,display_name,is_platform_role) VALUES ('SuperAdministrator','SuperAdministrador',true),('PlatformSupport','Suporte da plataforma',true),('PlatformFinance','Financeiro da plataforma',true),('PlatformAuditor','Auditoria da plataforma',true),('Owner','Titular',false),('Administrator','Administrador',false),('Collaborator','Colaborador',false),('Viewer','Leitor',false) ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['account.view','account.edit','members.view','members.manage','clients.view','clients.create','clients.edit','clients.delete','documents.view','documents.create','documents.edit','documents.delete','pdf.generate','billing:view','billing:manage','reports.view','exports.create']) code ON CONFLICT(code) DO NOTHING;
-- Compatibilidade idempotente: transfere vínculos antes de remover os slugs legados.
INSERT INTO orcafacil.role_permissions(role_id, permission_id, created_at, updated_at, is_deleted)
SELECT legacy_link.role_id, canonical.id, legacy_link.created_at, legacy_link.updated_at, legacy_link.is_deleted
FROM orcafacil.role_permissions legacy_link
JOIN orcafacil.permissions legacy ON legacy.id = legacy_link.permission_id
JOIN orcafacil.permissions canonical
  ON canonical.code = concat('billing', ':', split_part(legacy.code, '.', 2))
WHERE legacy.code IN (concat('billing', '.', 'view'), concat('billing', '.', 'manage'))
ON CONFLICT(role_id, permission_id) DO NOTHING;
DELETE FROM orcafacil.role_permissions
WHERE permission_id IN (
    SELECT id FROM orcafacil.permissions
    WHERE code IN (concat('billing', '.', 'view'), concat('billing', '.', 'manage'))
);
DELETE FROM orcafacil.permissions
WHERE code IN (concat('billing', '.', 'view'), concat('billing', '.', 'manage'));



-- Isolamento e sessões revogáveis (transição aditiva; UserId continua como autoria)
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_id uuid REFERENCES orcafacil.clients(id) ON DELETE SET NULL;
ALTER TABLE orcafacil.user_usage ADD COLUMN IF NOT EXISTS account_id uuid REFERENCES orcafacil.business_accounts(id);
ALTER TABLE orcafacil.public_quotes ADD COLUMN IF NOT EXISTS account_id uuid REFERENCES orcafacil.business_accounts(id);
ALTER TABLE orcafacil.payments ADD COLUMN IF NOT EXISTS billing_invoice_id uuid REFERENCES orcafacil.billing_invoices(id);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;
CREATE INDEX IF NOT EXISTS ix_documents_account_client ON orcafacil.documents(account_id,client_id);
CREATE INDEX IF NOT EXISTS ix_user_usage_account_period ON orcafacil.user_usage(account_id,period);
CREATE INDEX IF NOT EXISTS ix_public_quotes_account_created ON orcafacil.public_quotes(account_id,created_at);
CREATE UNIQUE INDEX IF NOT EXISTS ix_payments_idempotency_key ON orcafacil.payments(idempotency_key) WHERE idempotency_key IS NOT NULL;

INSERT INTO orcafacil.plan_versions(id,plan_id,version_number,monthly_price,annual_price,currency,valid_from,status,published_at)
SELECT md5(p.code || ':v1')::uuid,p.id,1,v.monthly,v.annual,'BRL',now(),'Published',now() FROM orcafacil.plans p JOIN (VALUES ('FREE',0::numeric,0::numeric),('PROFESSIONAL',24.90,249),('BUSINESS',49.90,499),('ENTERPRISE',0,0)) v(code,monthly,annual) ON v.code=p.code ON CONFLICT(plan_id,version_number) DO NOTHING;
INSERT INTO orcafacil.features(id,code,display_name,description,value_type,category)
SELECT md5(code)::uuid,code,name,name,type,category FROM (VALUES
('team.members_limit','Pessoas da equipe','Integer','Equipe'),('clients.active_limit','Clientes ativos','Integer','Clientes'),('services.active_limit','Serviços ativos','Integer','Serviços'),('documents.monthly_limit','Documentos mensais','Integer','Documentos'),('pdf.monthly_limit','PDFs mensais','Integer','Documentos'),('pdf.watermark','Marca OrçaFácil','Boolean','Documentos'),('branding.custom_logo','Logo próprio','Boolean','Marca'),('history.days_visible','Histórico visível','Integer','Histórico'),('templates.basic_limit','Modelos básicos','Integer','Modelos'),('templates.custom_enabled','Modelos personalizados','Boolean','Modelos'),('public_approval.enabled','Aprovação pública','Boolean','Documentos'),('public_approval.monthly_limit','Aprovações mensais','Integer','Documentos'),('document.convert_to_receipt','Conversão em recibo','Boolean','Documentos'),('sharing.whatsapp','Compartilhamento por WhatsApp','Boolean','Compartilhamento'),('sharing.public_link','Link público','Boolean','Compartilhamento'),('numbering.custom_prefix','Prefixo personalizado','Boolean','Documentos'),('commercial.pipeline','Pipeline comercial','Boolean','Comercial'),('commercial.followups','Acompanhamentos','Boolean','Comercial'),('commercial.metrics','Indicadores comerciais','Boolean','Comercial'),('reports.basic','Relatórios básicos','Boolean','Relatórios'),('reports.advanced','Relatórios avançados','Boolean','Relatórios'),('exports.csv','Exportação CSV','Boolean','Relatórios'),('audit.account','Auditoria da conta','Boolean','Auditoria'),('support.priority','Suporte prioritário','Boolean','Suporte')) f(code,name,type,category) ON CONFLICT(code) DO NOTHING;


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

-- RELEASE OPERACIONAL 4: recuperação segura e fila transacional
CREATE TABLE IF NOT EXISTS orcafacil.password_reset_tokens (id uuid PRIMARY KEY, user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, token_hash varchar(64) NOT NULL UNIQUE, created_at timestamptz NOT NULL, updated_at timestamptz, expires_at timestamptz NOT NULL, used_at timestamptz, revoked_at timestamptz, requested_correlation_id varchar(100) NOT NULL, requested_ip_hash varchar(64), user_agent_hash varchar(64), created_by varchar(60) NOT NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.email_outbox_messages (id uuid PRIMARY KEY, template_code varchar(80) NOT NULL, recipient_hash varchar(64) NOT NULL, recipient_masked varchar(254) NOT NULL, protected_recipient text NOT NULL, protected_payload text, status varchar(20) NOT NULL, priority varchar(20) NOT NULL, attempts integer NOT NULL DEFAULT 0, next_attempt_at timestamptz NOT NULL, processing_started_at timestamptz, processing_instance_id varchar(100), created_at timestamptz NOT NULL, updated_at timestamptz, sent_at timestamptz, dead_lettered_at timestamptz, last_error_code varchar(80), correlation_id varchar(100) NOT NULL, idempotency_key varchar(160) NOT NULL UNIQUE, is_deleted boolean NOT NULL DEFAULT false);


-- RELEASE OPERACIONAL 6: jornada comercial (migration 20260729000000)

CREATE TABLE IF NOT EXISTS orcafacil.document_revisions (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL REFERENCES orcafacil.documents(id) ON DELETE RESTRICT, version_number integer NOT NULL, status varchar(32) NOT NULL, created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, sent_at timestamptz, snapshot_hash varchar(128) NOT NULL, protected_snapshot text NOT NULL, template_code varchar(40) NOT NULL, branding_snapshot jsonb NOT NULL DEFAULT '{}', total numeric(18,2) NOT NULL, valid_until timestamptz, is_current boolean NOT NULL, version xid NOT NULL DEFAULT '0', is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_document_revisions_version ON orcafacil.document_revisions(account_id,document_id,version_number);
CREATE UNIQUE INDEX IF NOT EXISTS ux_document_revisions_current ON orcafacil.document_revisions(account_id,document_id,is_current) WHERE is_current = true;
CREATE INDEX IF NOT EXISTS ix_document_revisions_status_validity ON orcafacil.document_revisions(account_id,status,valid_until);
CREATE TABLE IF NOT EXISTS orcafacil.public_document_accesses (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, document_revision_id uuid NOT NULL REFERENCES orcafacil.document_revisions(id) ON DELETE RESTRICT, token_hash varchar(128) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, expires_at timestamptz NOT NULL, revoked_at timestamptz, last_viewed_at timestamptz, view_count integer NOT NULL DEFAULT 0, status varchar(24) NOT NULL, created_by_user_id uuid NOT NULL, version xid NOT NULL DEFAULT '0', is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_public_document_access_token_hash ON orcafacil.public_document_accesses(token_hash);
CREATE INDEX IF NOT EXISTS ix_public_document_access_document ON orcafacil.public_document_accesses(account_id,document_id,status);
CREATE INDEX IF NOT EXISTS ix_public_document_access_revision ON orcafacil.public_document_accesses(document_revision_id,status);
CREATE TABLE IF NOT EXISTS orcafacil.public_document_decisions (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, document_revision_id uuid NOT NULL, decision varchar(24) NOT NULL, customer_name varchar(180) NOT NULL, reason_code varchar(40), comment varchar(1000), created_at timestamptz NOT NULL, updated_at timestamptz, ip_hash varchar(128) NOT NULL, user_agent_hash varchar(128) NOT NULL, idempotency_key varchar(128) NOT NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_public_decision_revision ON orcafacil.public_document_decisions(account_id,document_revision_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_public_decision_idempotency ON orcafacil.public_document_decisions(account_id,idempotency_key);
ALTER TABLE orcafacil.public_document_decisions ADD COLUMN IF NOT EXISTS customer_contact varchar(254);
ALTER TABLE orcafacil.public_document_decisions ADD COLUMN IF NOT EXISTS desired_date date;
ALTER TABLE orcafacil.public_document_decisions ADD COLUMN IF NOT EXISTS accepted_terms boolean NOT NULL DEFAULT false;
CREATE TABLE IF NOT EXISTS orcafacil.commercial_follow_ups (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, document_revision_id uuid, channel text NOT NULL, result text NOT NULL, occurred_at timestamptz NOT NULL, note varchar(1000), created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_commercial_followups_document ON orcafacil.commercial_follow_ups(account_id,document_id,occurred_at);
CREATE TABLE IF NOT EXISTS orcafacil.work_orders (id uuid PRIMARY KEY, account_id uuid NOT NULL, source_document_id uuid, source_revision_id uuid, client_id uuid NOT NULL, number varchar(40) NOT NULL, title varchar(180) NOT NULL, description varchar(2000), status varchar(32) NOT NULL, scheduled_start timestamptz, scheduled_end timestamptz, started_at timestamptz, completed_at timestamptz, cancelled_at timestamptz, assigned_user_id uuid, address_snapshot jsonb NOT NULL DEFAULT '{}', client_snapshot jsonb NOT NULL DEFAULT '{}', items_snapshot jsonb NOT NULL DEFAULT '[]', total_snapshot numeric(18,2) NOT NULL, notes varchar(4000), created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, payment_received boolean NOT NULL DEFAULT false, payment_method varchar(80), version xid NOT NULL DEFAULT '0', is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_work_orders_number ON orcafacil.work_orders(account_id,number);
CREATE UNIQUE INDEX IF NOT EXISTS ux_work_orders_revision ON orcafacil.work_orders(account_id,source_revision_id) WHERE source_revision_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_work_orders_schedule ON orcafacil.work_orders(account_id,status,scheduled_start);
CREATE INDEX IF NOT EXISTS ix_work_orders_assignee ON orcafacil.work_orders(account_id,assigned_user_id,scheduled_start);
CREATE TABLE IF NOT EXISTS orcafacil.work_order_checklist_items (id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT, work_order_id uuid NOT NULL REFERENCES orcafacil.work_orders(id) ON DELETE CASCADE, description varchar(240) NOT NULL, position integer NOT NULL, is_completed boolean NOT NULL DEFAULT false, completed_at timestamptz, completed_by_user_id uuid REFERENCES orcafacil.users(id) ON DELETE SET NULL, completion_note varchar(1000), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_work_order_checklist_items_account_order_position ON orcafacil.work_order_checklist_items(account_id,work_order_id,position);

-- Follow-up comercial de orçamentos (2026-08-09)
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
CREATE INDEX IF NOT EXISTS ix_documents_account_id_next_follow_up_at ON orcafacil.documents(account_id, next_follow_up_at);

-- Captação de contatos comerciais pela Home (2026-08-11)
CREATE TABLE IF NOT EXISTS orcafacil.commercial_leads (
  id uuid PRIMARY KEY, name varchar(140) NOT NULL, company_name varchar(180), email varchar(254), phone varchar(40),
  segment varchar(100), monthly_budget_volume integer, message varchar(1200), consent_accepted boolean NOT NULL,
  source_page varchar(200) NOT NULL, status varchar(24) NOT NULL, converted_account_id uuid REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT,
  internal_notes varchar(3000), discard_reason varchar(500), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
  CONSTRAINT ck_commercial_leads_consent CHECK (consent_accepted = true),
  CONSTRAINT ck_commercial_leads_contact CHECK (email IS NOT NULL OR phone IS NOT NULL)
);
CREATE INDEX IF NOT EXISTS ix_commercial_leads_email ON orcafacil.commercial_leads(email);
CREATE INDEX IF NOT EXISTS ix_commercial_leads_status_created_at ON orcafacil.commercial_leads(status, created_at);

-- CRM comercial e suporte autenticado (2026-08-12)
ALTER TABLE orcafacil.commercial_leads ADD COLUMN IF NOT EXISTS converted_client_id uuid REFERENCES orcafacil.clients(id) ON DELETE RESTRICT;
CREATE INDEX IF NOT EXISTS ix_commercial_leads_converted_client_id ON orcafacil.commercial_leads(converted_client_id);
CREATE TABLE IF NOT EXISTS orcafacil.commercial_interactions (id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT, lead_id uuid REFERENCES orcafacil.commercial_leads(id) ON DELETE RESTRICT, client_id uuid REFERENCES orcafacil.clients(id) ON DELETE RESTRICT, document_id uuid REFERENCES orcafacil.documents(id) ON DELETE RESTRICT, type varchar(24) NOT NULL, channel varchar(24) NOT NULL, summary varchar(1200) NOT NULL, next_follow_up_at timestamptz, created_by_user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, completed_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_commercial_interactions_account_followup ON orcafacil.commercial_interactions(account_id,next_follow_up_at);
CREATE TABLE IF NOT EXISTS orcafacil.support_tickets (id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT, opened_by_user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, protocol varchar(24) NOT NULL UNIQUE, category varchar(24) NOT NULL, status varchar(24) NOT NULL, priority varchar(16) NOT NULL, subject varchar(180) NOT NULL, description varchar(5000) NOT NULL, internal_notes varchar(4000), resolved_at timestamptz, closed_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_support_tickets_account_status_created ON orcafacil.support_tickets(account_id,status,created_at);
CREATE TABLE IF NOT EXISTS orcafacil.support_ticket_messages (id uuid PRIMARY KEY, ticket_id uuid NOT NULL REFERENCES orcafacil.support_tickets(id) ON DELETE CASCADE, author_user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, body varchar(5000) NOT NULL, is_admin_reply boolean NOT NULL DEFAULT false, is_internal boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_support_ticket_messages_ticket_created ON orcafacil.support_ticket_messages(ticket_id,created_at);

-- Checklist operacional completo (2026-08-13). Seguro para bancos existentes.
ALTER TABLE IF EXISTS orcafacil.work_order_checklist_items
    ADD COLUMN IF NOT EXISTS details character varying(1000);
ALTER TABLE IF EXISTS orcafacil.work_order_checklist_items
    ADD COLUMN IF NOT EXISTS is_required boolean NOT NULL DEFAULT true;
CREATE INDEX IF NOT EXISTS ix_work_order_checklist_items_required_pending
    ON orcafacil.work_order_checklist_items (account_id, work_order_id, is_required, is_completed);

-- Templates comerciais por conta (2026-08-13). Defaults globais são somente leitura na aplicação.
CREATE TABLE IF NOT EXISTS orcafacil.commercial_message_templates (
    id uuid PRIMARY KEY, account_id uuid REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT,
    code varchar(80) NOT NULL, name varchar(140) NOT NULL, channel varchar(20) NOT NULL,
    subject varchar(180), body varchar(4000) NOT NULL, is_active boolean NOT NULL DEFAULT true,
    is_system boolean NOT NULL DEFAULT false, created_by_user_id uuid REFERENCES orcafacil.users(id) ON DELETE RESTRICT,
    created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
    CONSTRAINT ck_commercial_message_templates_channel CHECK (channel IN ('WhatsApp','Email','General'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_commercial_message_templates_account_id_code ON orcafacil.commercial_message_templates(account_id,code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_commercial_message_templates_system_code ON orcafacil.commercial_message_templates(code) WHERE account_id IS NULL;
INSERT INTO orcafacil.commercial_message_templates(id,account_id,code,name,channel,subject,body,is_active,is_system)
SELECT md5('message-template:' || code)::uuid,NULL,code,name,channel,subject,body,true,true FROM (VALUES
 ('quote-send','Envio de orçamento','WhatsApp',NULL,'Olá, {ClienteNome}! Preparei o orçamento {NumeroOrcamento}, no valor de {ValorTotal}. Você pode revisar a proposta em {LinkPublico}.'),
 ('sent-reminder','Lembrete após envio','WhatsApp',NULL,'Olá, {ClienteNome}! Conseguiu analisar o orçamento {NumeroOrcamento}? Estou à disposição para esclarecer qualquer ponto.'),
 ('view-reminder','Lembrete após visualização','WhatsApp',NULL,'Olá, {ClienteNome}! Vi que você acessou a proposta {NumeroOrcamento}. Posso ajudar com alguma dúvida?'),
 ('expiring','Proposta próxima do vencimento','Email','Sua proposta {NumeroOrcamento} vence em breve','Olá, {ClienteNome}. A proposta {NumeroOrcamento} é válida até {Validade}. Se precisar de ajustes, responda esta mensagem.'),
 ('expired','Proposta vencida','General',NULL,'Olá, {ClienteNome}. A validade da proposta {NumeroOrcamento} terminou. Posso preparar uma versão atualizada para você?'),
 ('change-request','Alteração solicitada','General',NULL,'Olá, {ClienteNome}! Recebemos sua solicitação de alteração na proposta {NumeroOrcamento} e vamos revisar os detalhes.'),
 ('approved','Proposta aprovada','General',NULL,'Obrigado pela aprovação, {ClienteNome}! O próximo passo é agendarmos a execução do serviço.'),
 ('work-order-schedule','Agendamento de OS','WhatsApp',NULL,'Olá, {ClienteNome}! Vamos combinar a melhor data para executar o serviço aprovado?'),
 ('friendly-charge','Cobrança amigável','General',NULL,'Olá, {ClienteNome}! Identificamos um pagamento pendente. Se já realizou, por favor desconsidere e nos envie o comprovante.'),
 ('thank-you','Agradecimento pós-venda','General',NULL,'Obrigado pela confiança, {ClienteNome}! Foi um prazer atender você. Conte com a {EmpresaNome}.')
) AS seed(code,name,channel,subject,body)
ON CONFLICT DO NOTHING;

-- Configurações avançadas, isoladas por conta (2026-08-13).
CREATE TABLE IF NOT EXISTS orcafacil.account_settings (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE CASCADE,
 state_registration text, municipal_registration text, whatsapp text, website text, postal_code text, address text, city text, state text, institutional_notes text,
 primary_color varchar(7), secondary_color varchar(7), accent_color varchar(7), logo_path text, compact_logo_path text, visual_signature text, document_footer text, short_institutional_text text,
 default_quote_validity_days integer NOT NULL DEFAULT 15, default_notes text, default_commercial_terms text, default_delivery_term text, default_send_message text,
 quote_prefix varchar(12) NOT NULL DEFAULT 'ORC', work_order_prefix varchar(12) NOT NULL DEFAULT 'OS', receipt_prefix varchar(12) NOT NULL DEFAULT 'REC', show_signature boolean NOT NULL DEFAULT true, show_bank_details boolean NOT NULL DEFAULT false, receipt_notice text,
 follow_up_after_sent_days integer NOT NULL DEFAULT 2, follow_up_after_viewed_days integer NOT NULL DEFAULT 1, expiration_alert_days integer NOT NULL DEFAULT 2, maximum_discount_percent numeric NOT NULL DEFAULT 0, desired_minimum_margin_percent numeric, discount_policy text, whatsapp_message text, email_message text, default_loss_reason text,
 accepted_payment_methods text, bank_name text, bank_branch text, bank_account text, beneficiary text, pix_key text, payment_instructions text, receipt_text text, collection_message text, notification_preferences_json jsonb NOT NULL DEFAULT '{}',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_account_settings_account_id ON orcafacil.account_settings(account_id);

-- Catálogo inteligente e precificação orientada (2026-08-13).
-- Tabelas centrais do fluxo quote-to-cash. Estas definições precisam anteceder
-- os ALTERs abaixo para também suportar uma instalação totalmente nova.
CREATE TABLE IF NOT EXISTS orcafacil.service_catalog_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT,
 code varchar(40), name varchar(180) NOT NULL, description varchar(1200), category_id uuid, unit_code varchar(24) NOT NULL DEFAULT 'service',
 standard_price numeric(18,2) NOT NULL DEFAULT 0, estimated_cost numeric(18,2) NOT NULL DEFAULT 0, suggested_duration_minutes integer,
 internal_notes varchar(2000), is_favorite boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, use_count integer NOT NULL DEFAULT 0,
 last_used_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
 version xid NOT NULL DEFAULT '0');
CREATE UNIQUE INDEX IF NOT EXISTS ux_service_catalog_items_account_code ON orcafacil.service_catalog_items(account_id,code) WHERE code IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_service_catalog_items_account_name ON orcafacil.service_catalog_items(account_id,name);

CREATE TABLE IF NOT EXISTS orcafacil.manual_payments (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT,
 work_order_id uuid REFERENCES orcafacil.work_orders(id) ON DELETE RESTRICT, document_id uuid REFERENCES orcafacil.documents(id) ON DELETE RESTRICT,
 client_id uuid NOT NULL REFERENCES orcafacil.clients(id) ON DELETE RESTRICT, amount numeric(18,2) NOT NULL,
 payment_method varchar(40) NOT NULL, paid_at timestamptz NOT NULL, notes varchar(1000), registered_by_user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT,
 idempotency_key varchar(128) NOT NULL, status varchar(24) NOT NULL DEFAULT 'Active', reversed_at timestamptz,
 reversed_by_user_id uuid REFERENCES orcafacil.users(id) ON DELETE RESTRICT, reversal_reason varchar(500),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_manual_payments_account_idempotency ON orcafacil.manual_payments(account_id,idempotency_key);
CREATE INDEX IF NOT EXISTS ix_manual_payments_account_work_order_paid ON orcafacil.manual_payments(account_id,work_order_id,paid_at);

CREATE TABLE IF NOT EXISTS orcafacil.receipts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id) ON DELETE RESTRICT,
 payment_id uuid NOT NULL REFERENCES orcafacil.manual_payments(id) ON DELETE RESTRICT, work_order_id uuid REFERENCES orcafacil.work_orders(id) ON DELETE RESTRICT,
 document_id uuid REFERENCES orcafacil.documents(id) ON DELETE RESTRICT, legacy_document_id uuid, client_id uuid NOT NULL REFERENCES orcafacil.clients(id) ON DELETE RESTRICT,
 number varchar(40) NOT NULL, issuer_snapshot jsonb NOT NULL DEFAULT '{}', client_snapshot jsonb NOT NULL DEFAULT '{}', service_snapshot jsonb NOT NULL DEFAULT '[]',
 amount numeric(18,2) NOT NULL, amount_in_words varchar(500) NOT NULL, payment_method varchar(40) NOT NULL, issued_at timestamptz NOT NULL,
 city varchar(180), notes varchar(1000), fiscal_notice varchar(500) NOT NULL, origin_type varchar(24) NOT NULL,
 service_description varchar(1000) NOT NULL, cancelled_at timestamptz, cancelled_by_user_id uuid REFERENCES orcafacil.users(id) ON DELETE RESTRICT,
 cancellation_reason varchar(500), pdf_storage_key varchar(500), sent_at timestamptz, last_shared_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_receipts_account_number ON orcafacil.receipts(account_id,number);
CREATE UNIQUE INDEX IF NOT EXISTS ux_receipts_account_payment ON orcafacil.receipts(account_id,payment_id);

ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS desired_margin_percentage numeric(5,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_delivery_term varchar(120);
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_notes varchar(2000);
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS tags varchar(500);
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS is_recurring boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS is_recommended boolean NOT NULL DEFAULT false;
CREATE INDEX IF NOT EXISTS ix_service_catalog_items_account_recurring ON orcafacil.service_catalog_items(account_id, is_recurring) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_service_catalog_items_account_recommended ON orcafacil.service_catalog_items(account_id, is_recommended) WHERE is_deleted = false AND is_active = true;
-- Governança da importação de cadastros (idempotente)
CREATE TABLE IF NOT EXISTS orcafacil.data_imports (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    type varchar(24) NOT NULL,
    file_name varchar(255) NOT NULL,
    uploaded_by_user_id uuid NOT NULL,
    status varchar(32) NOT NULL,
    total_rows integer NOT NULL DEFAULT 0,
    imported_rows integer NOT NULL DEFAULT 0,
    skipped_rows integer NOT NULL DEFAULT 0,
    failed_rows integer NOT NULL DEFAULT 0,
    completed_at timestamptz NULL,
    summary varchar(2000) NULL,
    staged_rows_json jsonb NULL,
    errors_json jsonb NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_data_imports_account_id_created_at ON orcafacil.data_imports (account_id, created_at);


-- Contratos recorrentes, faturamento, timeline e vínculo de OS (2026-08-13)
CREATE TABLE IF NOT EXISTS orcafacil.recurring_contracts (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, source_document_id uuid NULL, responsible_user_id uuid NULL,
 number varchar(40) NOT NULL, title varchar(180) NOT NULL, description varchar(2000), start_date date NOT NULL, end_date date,
 status varchar(32) NOT NULL DEFAULT 'Draft', recurring_amount numeric(18,2) NOT NULL, periodicity varchar(24) NOT NULL,
 custom_period_months integer, due_day integer NOT NULL, next_billing_date date, next_service_date date, commercial_terms varchar(4000),
 internal_notes varchar(4000), customer_notes varchar(4000), auto_renew boolean NOT NULL DEFAULT false, renewal_notice_days integer NOT NULL DEFAULT 30,
 response_sla_hours integer, execution_sla_hours integer, priority varchar(20) NOT NULL DEFAULT 'Normal', sla_uses_business_days boolean NOT NULL DEFAULT true,
 service_hours varchar(120), sla_notes varchar(1000), activated_at timestamptz, canceled_at timestamptz, cancellation_reason varchar(500),
 renewed_from_contract_id uuid, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
 CONSTRAINT ck_recurring_contract_due_day CHECK (due_day BETWEEN 1 AND 28), CONSTRAINT ck_recurring_contract_amount CHECK (recurring_amount > 0)
);
CREATE TABLE IF NOT EXISTS orcafacil.contract_items (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, service_catalog_item_id uuid, description varchar(500) NOT NULL,
 quantity numeric(18,4) NOT NULL, unit_price numeric(18,2) NOT NULL, checklist text, created_at timestamptz NOT NULL, updated_at timestamptz,
 is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT fk_contract_items_contract FOREIGN KEY(contract_id) REFERENCES orcafacil.recurring_contracts(id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS orcafacil.contract_payments (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, client_id uuid NOT NULL, competence date NOT NULL, due_date date NOT NULL,
 amount numeric(18,2) NOT NULL, payment_method varchar(40), status varchar(24) NOT NULL, paid_at timestamptz, notes varchar(1000), manual_payment_id uuid,
 created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false,
 CONSTRAINT fk_contract_payments_contract FOREIGN KEY(contract_id) REFERENCES orcafacil.recurring_contracts(id), CONSTRAINT ck_contract_payment_amount CHECK(amount > 0)
);
CREATE TABLE IF NOT EXISTS orcafacil.contract_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, user_id uuid NOT NULL, type varchar(50) NOT NULL, description varchar(1000) NOT NULL,
 related_entity_type varchar(50), related_entity_id uuid, related_url varchar(500), created_at timestamptz NOT NULL, updated_at timestamptz,
 is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT fk_contract_events_contract FOREIGN KEY(contract_id) REFERENCES orcafacil.recurring_contracts(id) ON DELETE CASCADE
);
ALTER TABLE orcafacil.work_orders ADD COLUMN IF NOT EXISTS contract_id uuid;
ALTER TABLE orcafacil.work_orders ADD COLUMN IF NOT EXISTS service_competence date;
CREATE UNIQUE INDEX IF NOT EXISTS ux_recurring_contract_account_number ON orcafacil.recurring_contracts(account_id,number);
CREATE UNIQUE INDEX IF NOT EXISTS ux_recurring_contract_source ON orcafacil.recurring_contracts(account_id,source_document_id) WHERE source_document_id IS NOT NULL AND is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_recurring_contract_client_status ON orcafacil.recurring_contracts(account_id,client_id,status);
CREATE INDEX IF NOT EXISTS ix_recurring_contract_end ON orcafacil.recurring_contracts(account_id,end_date);
CREATE INDEX IF NOT EXISTS ix_recurring_contract_next_billing ON orcafacil.recurring_contracts(account_id,next_billing_date);
CREATE INDEX IF NOT EXISTS ix_recurring_contract_next_service ON orcafacil.recurring_contracts(account_id,next_service_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contract_payment_competence ON orcafacil.contract_payments(account_id,contract_id,competence) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_contract_payment_due ON orcafacil.contract_payments(account_id,status,due_date);
CREATE INDEX IF NOT EXISTS ix_contract_event_timeline ON orcafacil.contract_events(account_id,contract_id,created_at);
CREATE UNIQUE INDEX IF NOT EXISTS ux_work_order_contract_competence ON orcafacil.work_orders(account_id,contract_id,service_competence) WHERE contract_id IS NOT NULL AND is_deleted=false;
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
CREATE TABLE IF NOT EXISTS orcafacil.document_sequences (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, document_type varchar(30) NOT NULL, current_number bigint NOT NULL DEFAULT 0,
 prefix varchar(12) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sequences_account_type ON orcafacil.document_sequences(account_id,document_type);
INSERT INTO orcafacil.document_sequences(id,account_id,document_type,current_number,prefix,created_at,is_deleted)
SELECT gen_random_uuid(),account_id,type,COALESCE(MAX(CASE WHEN number ~ ('^' || CASE WHEN type='Receipt' THEN 'REC' ELSE 'ORC' END || '-[0-9]+$') THEN substring(number from length(CASE WHEN type='Receipt' THEN 'REC' ELSE 'ORC' END)+2)::bigint ELSE 0 END),0),CASE WHEN type='Receipt' THEN 'REC' ELSE 'ORC' END,now(),false
FROM orcafacil.documents WHERE account_id IS NOT NULL AND number IS NOT NULL AND is_deleted=false GROUP BY account_id,type
ON CONFLICT(account_id,document_type) DO UPDATE SET current_number=GREATEST(orcafacil.document_sequences.current_number,EXCLUDED.current_number),updated_at=now();
CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number ON orcafacil.documents(account_id,type,number) WHERE account_id IS NOT NULL AND is_deleted=false;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_periodicity integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS suggested_monthly_price numeric(18,2);
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS estimated_monthly_cost numeric(18,2);
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_response_sla_hours integer;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_execution_sla_hours integer;
ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_checklist text;

-- Go-live assistido V1 (idempotente)
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS related_page varchar(300);
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS correlation_id varchar(100);
ALTER TABLE orcafacil.support_tickets ADD COLUMN IF NOT EXISTS browser_info varchar(500);
CREATE TABLE IF NOT EXISTS orcafacil.user_feedback (id uuid PRIMARY KEY, account_id uuid NULL, user_id uuid NULL, page_url varchar(500) NOT NULL, rating varchar(32) NOT NULL, message varchar(2000), browser_info varchar(500), correlation_id varchar(100), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.knowledge_base_articles (id uuid PRIMARY KEY, title varchar(180) NOT NULL, slug varchar(180) NOT NULL UNIQUE, summary varchar(500) NOT NULL, content varchar(12000) NOT NULL, category varchar(80) NOT NULL, audience varchar(24) NOT NULL DEFAULT 'All', is_published boolean NOT NULL DEFAULT false, display_order integer NOT NULL DEFAULT 0, published_at timestamptz NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.release_notes (id uuid PRIMARY KEY, version varchar(30) NOT NULL, title varchar(180) NOT NULL, description varchar(5000) NOT NULL, released_at timestamptz NOT NULL, category varchar(32) NOT NULL, is_published boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['Support.View','Support.CreateTicket','Support.ManageTickets','Feedback.View','Feedback.Create','KnowledgeBase.Manage','ReleaseNotes.Manage','SetupChecklist.View','SetupChecklist.Manage','Admin.Access']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted) SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE (r.code IN ('Owner','Administrator') AND p.code IN ('Support.View','Support.CreateTicket','Feedback.Create','SetupChecklist.View','SetupChecklist.Manage')) OR (r.code IN ('Collaborator','Viewer') AND p.code IN ('Support.View','Support.CreateTicket','Feedback.Create','SetupChecklist.View')) OR (r.code IN ('SuperAdministrator','PlatformSupport') AND p.code IN ('Support.View','Support.CreateTicket','Support.ManageTickets','Feedback.View','Feedback.Create','KnowledgeBase.Manage','ReleaseNotes.Manage','SetupChecklist.View','SetupChecklist.Manage','Admin.Access')) ON CONFLICT(role_id,permission_id) DO NOTHING;

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
ALTER TABLE orcafacil.automation_rules ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
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
CREATE TABLE IF NOT EXISTS orcafacil.data_subject_requests (
    id uuid PRIMARY KEY,
    requester_user_id uuid NOT NULL,
    account_id uuid NOT NULL,
    type varchar(40) NOT NULL,
    description varchar(4000) NOT NULL,
    status varchar(40) NOT NULL,
    requested_at timestamptz NOT NULL,
    verified_at timestamptz NULL,
    due_at timestamptz NOT NULL,
    completed_at timestamptz NULL,
    rejected_at timestamptz NULL,
    rejection_reason text NULL,
    assigned_to_user_id uuid NULL,
    correlation_id uuid NOT NULL,
    delivery_file_id uuid NULL,
    client_id uuid NULL,
    resolution_notes text NULL,
    reviewed_at timestamptz NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false);
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

-- Sprint 19: productivity permissions (idempotent, no user/account recreation).
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

-- Sprint 20 / Analytics V2.1: idempotent, tenant-scoped and data preserving.
CREATE TABLE IF NOT EXISTS orcafacil.business_goals (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL, business_unit_id uuid, team_id uuid, assigned_to_user_id uuid, name varchar(160) NOT NULL, goal_type integer NOT NULL, period_type integer NOT NULL, start_date date NOT NULL, end_date date NOT NULL, target_value numeric(18,2) NOT NULL CHECK(target_value >= 0), current_value numeric(18,2) NOT NULL DEFAULT 0, status integer NOT NULL DEFAULT 0, created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CHECK(end_date >= start_date));
CREATE INDEX IF NOT EXISTS ix_business_goals_account_period ON orcafacil.business_goals(account_id,start_date,end_date);
CREATE TABLE IF NOT EXISTS orcafacil.goal_progress_snapshots (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL, goal_id uuid NOT NULL, reference_date date NOT NULL, current_value numeric(18,2) NOT NULL, progress_percentage numeric(8,2) NOT NULL, status integer NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_goal_progress_period ON orcafacil.goal_progress_snapshots(account_id,goal_id,reference_date);
CREATE TABLE IF NOT EXISTS orcafacil.analytics_snapshots (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL, frequency varchar(16) NOT NULL, period_start date NOT NULL, period_end date NOT NULL, calculated_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_analytics_snapshot_period ON orcafacil.analytics_snapshots(account_id,frequency,period_start,period_end);
CREATE TABLE IF NOT EXISTS orcafacil.analytics_snapshot_items (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL, snapshot_id uuid NOT NULL, metric_code varchar(80) NOT NULL, value numeric(18,2) NOT NULL, explanation text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_analytics_snapshot_item ON orcafacil.analytics_snapshot_items(account_id,snapshot_id,metric_code);
CREATE TABLE IF NOT EXISTS orcafacil.forecast_snapshots (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL, forecast_type varchar(32) NOT NULL, reference_date date NOT NULL, horizon_days integer NOT NULL, forecast_value numeric(18,2) NOT NULL, confidence varchar(24) NOT NULL, explanation text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_forecast_snapshot_period ON orcafacil.forecast_snapshots(account_id,forecast_type,reference_date,horizon_days);
CREATE TABLE IF NOT EXISTS orcafacil.data_quality_findings (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL, rule_code varchar(80) NOT NULL, severity integer NOT NULL, entity_type varchar(40) NOT NULL, entity_id uuid NOT NULL, title text NOT NULL, description text NOT NULL, action_url varchar(500) NOT NULL, status integer NOT NULL DEFAULT 0, detected_at timestamptz NOT NULL, resolved_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_data_quality_finding ON orcafacil.data_quality_findings(account_id,rule_code,entity_type,entity_id);
CREATE TABLE IF NOT EXISTS orcafacil.dashboard_widget_preferences (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), account_id uuid NOT NULL, user_id uuid NOT NULL, widget_code varchar(80) NOT NULL, position integer NOT NULL, is_visible boolean NOT NULL DEFAULT true, is_favorite boolean NOT NULL DEFAULT false, default_period varchar(24) NOT NULL DEFAULT 'month', filters_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_dashboard_widget_user ON orcafacil.dashboard_widget_preferences(account_id,user_id,widget_code);
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['Analytics.View','Analytics.Executive','Analytics.Financial','Analytics.Operational','Analytics.Forecast','Analytics.Export','Goals.View','Goals.Manage','DataQuality.View','DataQuality.Manage','AccountHealth.View','ExecutiveAlerts.View','Dashboard.Customize']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted) SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE r.code IN ('Owner','Administrator') AND p.code IN ('Analytics.View','Analytics.Executive','Analytics.Financial','Analytics.Operational','Analytics.Forecast','Analytics.Export','Goals.View','Goals.Manage','DataQuality.View','DataQuality.Manage','AccountHealth.View','ExecutiveAlerts.View','Dashboard.Customize') ON CONFLICT(role_id,permission_id) DO NOTHING;

-- Sprint 21 / CRM V2.2 (idempotent, data preserving)
CREATE TABLE IF NOT EXISTS orcafacil.client_relationship_profiles (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, status varchar(24) NOT NULL, status_reason varchar(500) NOT NULL, commercial_temperature varchar(16) NOT NULL, last_interaction_at timestamptz NULL, next_action_at timestamptz NULL, commercial_owner_user_id uuid NULL, success_owner_user_id uuid NULL, source varchar(120) NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_crm_profile_account_client ON orcafacil.client_relationship_profiles(account_id, client_id);
CREATE TABLE IF NOT EXISTS orcafacil.client_interactions (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, user_id uuid NOT NULL, interaction_type varchar(24) NOT NULL, title varchar(180) NOT NULL, description varchar(5000) NOT NULL, interaction_date timestamptz NOT NULL, next_action_date timestamptz NULL, outcome varchar(1000) NULL, restricted_visibility boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_client_interactions_timeline ON orcafacil.client_interactions(account_id, client_id, interaction_date DESC);
CREATE TABLE IF NOT EXISTS orcafacil.client_health_scores (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, score integer NOT NULL CHECK(score BETWEEN 0 AND 100), classification varchar(32) NOT NULL, explanation_json jsonb NOT NULL, calculated_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_health_account_client ON orcafacil.client_health_scores(account_id, client_id, calculated_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.communication_opt_outs (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, channel varchar(32) NULL, commercial_communications boolean NOT NULL DEFAULT true, reason varchar(500) NOT NULL, opted_out_at timestamptz NOT NULL, registered_by_user_id uuid NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_opt_out_account_client_channel ON orcafacil.communication_opt_outs(account_id, client_id, channel);
CREATE TABLE IF NOT EXISTS orcafacil.nps_responses (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, survey_id uuid NOT NULL, score integer NOT NULL CHECK(score BETWEEN 0 AND 10), comment varchar(3000) NULL, answered_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_nps_survey_client ON orcafacil.nps_responses(account_id, survey_id, client_id);
CREATE TABLE IF NOT EXISTS orcafacil.retention_risk_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, factor_code varchar(80) NOT NULL, level varchar(16) NOT NULL, reason varchar(500) NOT NULL, recommended_action varchar(500) NOT NULL, detected_at timestamptz NOT NULL, resolved_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_retention_open ON orcafacil.retention_risk_events(account_id, client_id, factor_code) WHERE resolved_at IS NULL AND is_deleted = false;
CREATE TABLE IF NOT EXISTS orcafacil.crm_opportunities (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, kind varchar(16) NOT NULL, reason varchar(1000) NOT NULL, next_action varchar(500) NOT NULL, next_action_at timestamptz NOT NULL, status varchar(16) NOT NULL, discard_reason varchar(500) NULL, converted_document_id uuid NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_opportunity_account_client_status ON orcafacil.crm_opportunities(account_id, client_id, status);
BEGIN;
CREATE TABLE IF NOT EXISTS orcafacil.contract_sla_policies (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, name varchar(120) NOT NULL, priority varchar(16) NOT NULL, response_time_minutes integer NOT NULL CHECK(response_time_minutes>0), resolution_time_minutes integer NOT NULL CHECK(resolution_time_minutes>0), business_hours_only boolean NOT NULL DEFAULT false, business_days_json jsonb NOT NULL DEFAULT '[1,2,3,4,5]', start_time time NOT NULL DEFAULT '08:00', end_time time NOT NULL DEFAULT '18:00', is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_sla_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, work_order_id uuid, support_ticket_id uuid, sla_policy_id uuid NOT NULL, event_type varchar(48) NOT NULL, occurred_at timestamptz NOT NULL, due_at timestamptz, idempotency_key varchar(180), details varchar(1000), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.service_level_breaches (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, sla_policy_id uuid NOT NULL, work_order_id uuid, breach_type varchar(32) NOT NULL, due_at timestamptz NOT NULL, detected_at timestamptz NOT NULL, resolved_at timestamptz, idempotency_key varchar(180) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_warranty_terms (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, client_id uuid NOT NULL, work_order_id uuid, service_catalog_item_id uuid, coverage varchar(500) NOT NULL, conditions varchar(2000), start_date date NOT NULL, end_date date NOT NULL CHECK(end_date>=start_date), status varchar(20) NOT NULL, cancellation_reason varchar(500), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_preventive_schedules (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, client_id uuid NOT NULL, name varchar(160) NOT NULL, description varchar(2000) NOT NULL, frequency varchar(24) NOT NULL, interval integer NOT NULL CHECK(interval>0), start_date date NOT NULL, end_date date, next_run_date date NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_recurrence_runs (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, preventive_schedule_id uuid, run_type varchar(32) NOT NULL, period_start date NOT NULL, period_end date NOT NULL, idempotency_key varchar(200) NOT NULL, status varchar(24) NOT NULL, generated_entity_id uuid, error_summary varchar(500), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_usage_allowances (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, usage_type varchar(40) NOT NULL, period_start date NOT NULL, period_end date NOT NULL, allowance_quantity numeric(18,4) NOT NULL CHECK(allowance_quantity>=0), used_quantity numeric(18,4) NOT NULL DEFAULT 0 CHECK(used_quantity>=0), unit varchar(30) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_amendments (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, amendment_number varchar(40) NOT NULL, type varchar(40) NOT NULL, description varchar(2000) NOT NULL, effective_date date NOT NULL, previous_snapshot_json jsonb NOT NULL, new_snapshot_json jsonb NOT NULL, approved_by_user_id uuid, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_adjustments (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, adjustment_type varchar(32) NOT NULL, percent numeric(10,4), amount numeric(18,2), reason varchar(1000) NOT NULL, effective_date date NOT NULL, old_value numeric(18,2) NOT NULL, new_value numeric(18,2) NOT NULL CHECK(new_value>=0), status varchar(24) NOT NULL, created_by_user_id uuid NOT NULL, approved_by_user_id uuid, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_renewal_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, event_type varchar(32) NOT NULL, occurred_at timestamptz NOT NULL, approved_by_user_id uuid, reason varchar(1000), idempotency_key varchar(180) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.contract_health_snapshots (id uuid PRIMARY KEY, account_id uuid NOT NULL, contract_id uuid NOT NULL, score integer NOT NULL CHECK(score BETWEEN 0 AND 100), classification varchar(30) NOT NULL, positive_factors_json jsonb NOT NULL, risk_factors_json jsonb NOT NULL, next_action varchar(500) NOT NULL, calculated_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contract_sla_event_key ON orcafacil.contract_sla_events(account_id,idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_service_level_breach_key ON orcafacil.service_level_breaches(account_id,idempotency_key);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contract_recurrence_key ON orcafacil.contract_recurrence_runs(account_id,idempotency_key);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contract_renewal_key ON orcafacil.contract_renewal_events(account_id,idempotency_key);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contract_usage_period ON orcafacil.contract_usage_allowances(account_id,contract_id,usage_type,period_start);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contract_amendment_number ON orcafacil.contract_amendments(account_id,contract_id,amendment_number);
CREATE INDEX IF NOT EXISTS ix_contract_sla_active ON orcafacil.contract_sla_policies(account_id,contract_id,is_active);
CREATE INDEX IF NOT EXISTS ix_contract_warranty_expiry ON orcafacil.contract_warranty_terms(account_id,status,end_date);
CREATE INDEX IF NOT EXISTS ix_contract_preventive_due ON orcafacil.contract_preventive_schedules(account_id,is_active,next_run_date);
COMMIT;
BEGIN;
CREATE TABLE IF NOT EXISTS orcafacil.suppliers (id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(180) NOT NULL, legal_name varchar(180), document_number varchar(32), email varchar(254), phone varchar(32), whatsapp varchar(32), website varchar(500), address_line varchar(300), city varchar(100), state varchar(40), zip_code varchar(16), category varchar(80), status varchar(24) NOT NULL, notes text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.material_units (id uuid PRIMARY KEY, account_id uuid, name varchar(80) NOT NULL, symbol varchar(16) NOT NULL, is_global boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.material_categories (id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(100) NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.materials (id uuid PRIMARY KEY, account_id uuid NOT NULL, code varchar(50) NOT NULL, name varchar(180) NOT NULL, description text, category_id uuid NOT NULL, unit_id uuid NOT NULL, default_cost numeric(18,4) NOT NULL DEFAULT 0 CHECK(default_cost>=0), default_sale_price numeric(18,2) NOT NULL DEFAULT 0 CHECK(default_sale_price>=0), minimum_stock numeric(18,4) NOT NULL DEFAULT 0 CHECK(minimum_stock>=0), is_stock_controlled boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.material_supplier_prices (id uuid PRIMARY KEY, account_id uuid NOT NULL, supplier_id uuid NOT NULL, material_id uuid NOT NULL, unit_cost numeric(18,4) NOT NULL CHECK(unit_cost>=0), minimum_quantity numeric(18,4) NOT NULL DEFAULT 1 CHECK(minimum_quantity>0), lead_time_days integer NOT NULL DEFAULT 0 CHECK(lead_time_days>=0), valid_from timestamptz NOT NULL, valid_until timestamptz, is_preferred boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.inventory_locations (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid, name varchar(120) NOT NULL, description text, is_default boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.inventory_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, material_id uuid NOT NULL, inventory_location_id uuid NOT NULL, quantity_on_hand numeric(18,4) NOT NULL DEFAULT 0, quantity_reserved numeric(18,4) NOT NULL DEFAULT 0 CHECK(quantity_reserved>=0 AND quantity_reserved<=quantity_on_hand), average_cost numeric(18,4) NOT NULL DEFAULT 0 CHECK(average_cost>=0), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.inventory_stock_movements (id uuid PRIMARY KEY, account_id uuid NOT NULL, material_id uuid NOT NULL, inventory_location_id uuid NOT NULL, work_order_id uuid, purchase_order_id uuid, movement_type varchar(32) NOT NULL, quantity numeric(18,4) NOT NULL CHECK(quantity>0), unit_cost numeric(18,4), reason text, created_by_user_id uuid NOT NULL, reverses_movement_id uuid, idempotency_key varchar(180), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.inventory_reservations (id uuid PRIMARY KEY, account_id uuid NOT NULL, work_order_id uuid NOT NULL, material_id uuid NOT NULL, inventory_location_id uuid NOT NULL, quantity numeric(18,4) NOT NULL CHECK(quantity>0), consumed_quantity numeric(18,4) NOT NULL DEFAULT 0 CHECK(consumed_quantity>=0), is_released boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.purchase_requests (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid, requested_by_user_id uuid NOT NULL, work_order_id uuid, document_id uuid, status varchar(32) NOT NULL, reason text NOT NULL, needed_by_date timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.purchase_request_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, purchase_request_id uuid NOT NULL, material_id uuid, description varchar(500) NOT NULL, quantity numeric(18,4) NOT NULL CHECK(quantity>0), estimated_unit_cost numeric(18,4) CHECK(estimated_unit_cost>=0), preferred_supplier_id uuid, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.purchase_orders (id uuid PRIMARY KEY, account_id uuid NOT NULL, supplier_id uuid NOT NULL, business_unit_id uuid, purchase_request_id uuid, purchase_order_number varchar(50) NOT NULL, status varchar(32) NOT NULL, issue_date timestamptz NOT NULL, expected_delivery_date timestamptz, subtotal numeric(18,2) NOT NULL CHECK(subtotal>=0), discount_amount numeric(18,2) NOT NULL DEFAULT 0 CHECK(discount_amount>=0), total_amount numeric(18,2) NOT NULL CHECK(total_amount>=0), notes text, created_by_user_id uuid NOT NULL, approved_by_user_id uuid, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.purchase_order_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, purchase_order_id uuid NOT NULL, material_id uuid, description varchar(500) NOT NULL, quantity numeric(18,4) NOT NULL CHECK(quantity>0), unit_cost numeric(18,4) NOT NULL CHECK(unit_cost>=0), total_cost numeric(18,2) NOT NULL CHECK(total_cost>=0), received_quantity numeric(18,4) NOT NULL DEFAULT 0 CHECK(received_quantity>=0 AND received_quantity<=quantity), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.cost_compositions (id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(180) NOT NULL, description text, target_type varchar(32) NOT NULL, target_id uuid, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.cost_composition_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, composition_id uuid NOT NULL, material_id uuid, description varchar(500) NOT NULL, cost_type varchar(40) NOT NULL, quantity numeric(18,4) NOT NULL CHECK(quantity>0), unit_cost numeric(18,4) NOT NULL CHECK(unit_cost>=0), total_cost numeric(18,2) NOT NULL CHECK(total_cost>=0), markup_percent numeric(10,4) NOT NULL DEFAULT 0, sale_price numeric(18,2) NOT NULL CHECK(sale_price>=0), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.document_cost_snapshots (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, estimated_cost numeric(18,2) NOT NULL CHECK(estimated_cost>=0), sale_price numeric(18,2) NOT NULL CHECK(sale_price>=0), items_json jsonb NOT NULL, calculated_at timestamptz NOT NULL, calculated_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.document_margin_snapshots (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, cost_snapshot_id uuid NOT NULL, estimated_margin_percent numeric(10,4) NOT NULL, requires_approval boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.margin_policies (id uuid PRIMARY KEY, account_id uuid NOT NULL, business_unit_id uuid, name varchar(120) NOT NULL, minimum_margin_percent numeric(10,4) NOT NULL, warning_margin_percent numeric(10,4) NOT NULL, requires_approval_below_minimum boolean NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_supplier_document ON orcafacil.suppliers(account_id,document_number) WHERE document_number IS NOT NULL AND is_deleted=false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_material_code ON orcafacil.materials(account_id,code) WHERE is_deleted=false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_inventory_item ON orcafacil.inventory_items(account_id,material_id,inventory_location_id) WHERE is_deleted=false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_stock_movement_idempotency ON orcafacil.inventory_stock_movements(account_id,idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_active_reservation ON orcafacil.inventory_reservations(account_id,work_order_id,material_id,inventory_location_id) WHERE is_released=false AND is_deleted=false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_purchase_order_number ON orcafacil.purchase_orders(account_id,purchase_order_number) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_supplier_price_current ON orcafacil.material_supplier_prices(account_id,material_id,valid_until,is_preferred) WHERE is_deleted=false;
COMMIT;

-- Sprint 24 / V2.5: execute the idempotent advanced-finance patch after this baseline.
-- Source: database/patch_sprint24_advanced_finance_v25.sql (kept separate for safe upgrades).

-- Sprint 26 is maintained in database/patch_sprint26_partner_portal.sql and applied by release patch.

-- Sprint 26 / V2.7 - rede de parceiros. Additive and safe for existing tenants.
CREATE SCHEMA IF NOT EXISTS orcafacil;
CREATE TABLE IF NOT EXISTS orcafacil.partner_profiles (id uuid PRIMARY KEY, account_id uuid NOT NULL, supplier_id uuid NULL, name varchar(180) NOT NULL, legal_name varchar(180), document_number varchar(30), email varchar(254), phone varchar(30), whatsapp varchar(30), website varchar(300), category varchar(32) NOT NULL, status varchar(24) NOT NULL, rating_average numeric(3,2) NOT NULL DEFAULT 0, notes text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_contacts (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL REFERENCES orcafacil.partner_profiles(id), name varchar(180) NOT NULL, email varchar(254) NOT NULL, phone varchar(30), whatsapp varchar(30), role varchar(100), is_primary boolean NOT NULL DEFAULT false, can_access_portal boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_portal_users (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL REFERENCES orcafacil.partner_profiles(id), partner_contact_id uuid NOT NULL REFERENCES orcafacil.partner_contacts(id), email_normalized varchar(254) NOT NULL, is_active boolean NOT NULL DEFAULT true, access_revoked_at timestamptz, last_login_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_portal_invitations (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, partner_contact_id uuid NOT NULL, token_hash char(64) NOT NULL, expires_at timestamptz NOT NULL, accepted_at timestamptz, revoked_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_portal_sessions (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, partner_portal_user_id uuid NOT NULL, token_hash char(64) NOT NULL, expires_at timestamptz NOT NULL, revoked_at timestamptz, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_portal_security_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid, event_type varchar(64) NOT NULL, ip_address_hash varchar(64), user_agent varchar(300), occurred_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.outsourcing_requests (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid, work_order_id uuid, document_id uuid, business_unit_id uuid, requested_by_user_id uuid NOT NULL, status varchar(24) NOT NULL, title varchar(180) NOT NULL, description text NOT NULL, needed_by_date timestamptz, budget_limit numeric(18,2), internal_notes text, cancellation_reason text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.outsourcing_request_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, outsourcing_request_id uuid NOT NULL REFERENCES orcafacil.outsourcing_requests(id), service_catalog_item_id uuid, material_id uuid, description varchar(500) NOT NULL, quantity numeric(18,4) NOT NULL CHECK (quantity > 0), unit varchar(30) NOT NULL, expected_unit_cost numeric(18,2), created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.outsourcing_quotes (id uuid PRIMARY KEY, account_id uuid NOT NULL, outsourcing_request_id uuid NOT NULL REFERENCES orcafacil.outsourcing_requests(id), partner_id uuid NOT NULL REFERENCES orcafacil.partner_profiles(id), status varchar(24) NOT NULL, submitted_at timestamptz, expires_at timestamptz NOT NULL, total_amount numeric(18,2) NOT NULL CHECK(total_amount >= 0), lead_time_days int NOT NULL CHECK(lead_time_days >= 0), partner_notes text, internal_decision_notes text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.outsourcing_assignments (id uuid PRIMARY KEY, account_id uuid NOT NULL, work_order_id uuid NOT NULL, outsourcing_request_id uuid NOT NULL, outsourcing_quote_id uuid NOT NULL, partner_id uuid NOT NULL, status varchar(24) NOT NULL, assigned_at timestamptz NOT NULL, accepted_at timestamptz, rejected_at timestamptz, started_at timestamptz, completed_at timestamptz, canceled_at timestamptz, agreed_amount numeric(18,2) NOT NULL CHECK(agreed_amount >= 0), agreed_due_date timestamptz, decision_reason text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_capabilities (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, service_catalog_item_id uuid, capability_name varchar(180) NOT NULL, description text, default_cost numeric(18,2) NOT NULL CHECK(default_cost>=0), default_lead_time_days int NOT NULL CHECK(default_lead_time_days>=0), is_active boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_service_areas (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, city varchar(120) NOT NULL, state varchar(2) NOT NULL, region varchar(120), radius_km numeric(9,2) NOT NULL CHECK(radius_km>=0), is_active boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_documents (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, file_asset_id uuid NOT NULL, document_type varchar(80) NOT NULL, status varchar(24) NOT NULL, expiration_date timestamptz, validated_at timestamptz, validated_by_user_id uuid, notes text, rejection_reason text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.outsourcing_quote_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, outsourcing_quote_id uuid NOT NULL, description varchar(500) NOT NULL, quantity numeric(18,4) NOT NULL CHECK(quantity>0), unit_amount numeric(18,2) NOT NULL CHECK(unit_amount>=0), total_amount numeric(18,2) NOT NULL CHECK(total_amount>=0), notes text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_work_order_updates (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, work_order_id uuid NOT NULL, outsourcing_assignment_id uuid NOT NULL, update_type varchar(40) NOT NULL, message text NOT NULL, is_visible_to_client boolean NOT NULL DEFAULT false, submitted_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_work_order_evidences (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, work_order_id uuid NOT NULL, file_asset_id uuid NOT NULL, evidence_type varchar(40) NOT NULL, description text, is_visible_to_client boolean NOT NULL DEFAULT false, submitted_at timestamptz NOT NULL, reviewed_at timestamptz, reviewed_by_user_id uuid, status varchar(24) NOT NULL, rejection_reason text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_payment_requests (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, work_order_id uuid NOT NULL, outsourcing_assignment_id uuid NOT NULL, amount numeric(18,2) NOT NULL CHECK(amount>0), status varchar(24) NOT NULL, requested_at timestamptz NOT NULL, approved_at timestamptz, paid_at timestamptz, payable_id uuid, notes text, decision_reason text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_cost_snapshots (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, work_order_id uuid NOT NULL, document_id uuid, outsourcing_assignment_id uuid NOT NULL, contracted_amount numeric(18,2) NOT NULL CHECK(contracted_amount>=0), captured_at timestamptz NOT NULL, captured_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_ratings (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, work_order_id uuid NOT NULL, rated_by_user_id uuid NOT NULL, quality_score int NOT NULL CHECK(quality_score BETWEEN 1 AND 5), punctuality_score int NOT NULL CHECK(punctuality_score BETWEEN 1 AND 5), communication_score int NOT NULL CHECK(communication_score BETWEEN 1 AND 5), deadline_score int NOT NULL CHECK(deadline_score BETWEEN 1 AND 5), documentation_score int NOT NULL CHECK(documentation_score BETWEEN 1 AND 5), cost_benefit_score int NOT NULL CHECK(cost_benefit_score BETWEEN 1 AND 5), client_satisfaction_score int NOT NULL CHECK(client_satisfaction_score BETWEEN 1 AND 5), comment text, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.partner_terms_acceptances (id uuid PRIMARY KEY, account_id uuid NOT NULL, partner_id uuid NOT NULL, partner_portal_user_id uuid NOT NULL, terms_version varchar(40) NOT NULL, accepted_at timestamptz NOT NULL, ip_address_hash varchar(64) NOT NULL, user_agent varchar(300) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_partner_invitation_token_hash ON orcafacil.partner_portal_invitations(token_hash);
CREATE UNIQUE INDEX IF NOT EXISTS ux_partner_session_token_hash ON orcafacil.partner_portal_sessions(token_hash);
CREATE INDEX IF NOT EXISTS ix_partner_tenant ON orcafacil.partner_profiles(account_id, status) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_partner_quote_scope ON orcafacil.outsourcing_quotes(account_id, partner_id, status) WHERE is_deleted=false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_active_outsourced_work_order ON orcafacil.outsourcing_assignments(account_id, work_order_id) WHERE is_deleted=false AND status NOT IN ('Canceled','Rejected');

-- Sprint 27 / V2.8 - API pública segura.
CREATE TABLE IF NOT EXISTS orcafacil.api_request_logs (id uuid PRIMARY KEY, account_id uuid NOT NULL, api_key_id uuid NOT NULL, route varchar(300) NOT NULL, method varchar(10) NOT NULL, status_code integer NOT NULL, elapsed_milliseconds bigint NOT NULL, ip_address varchar(64), user_agent varchar(300), correlation_id varchar(100) NOT NULL, error_code varchar(60), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.api_idempotency_keys (id uuid PRIMARY KEY, account_id uuid NOT NULL, api_key_id uuid NOT NULL, key_hash varchar(64) NOT NULL, request_hash varchar(64) NOT NULL, response_status_code integer NOT NULL, response_json jsonb NOT NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_account_created ON orcafacil.api_request_logs(account_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs_key_created ON orcafacil.api_request_logs(api_key_id, created_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_api_idempotency_tenant_key ON orcafacil.api_idempotency_keys(account_id, api_key_id, key_hash) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_api_idempotency_expiry ON orcafacil.api_idempotency_keys(expires_at);
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['DeveloperPortal.View','DeveloperPortal.Manage','ApiKeys.View','ApiLogs.View','Webhooks.Replay','ExternalApps.View','ExternalApps.Manage','Connectors.View','Connectors.Manage','IntegrationHealth.View','Admin.ApiGlobalView']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted) SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE r.code IN ('Owner','Administrator') AND p.code IN ('DeveloperPortal.View','DeveloperPortal.Manage','ApiKeys.View','ApiLogs.View','Webhooks.Replay','ExternalApps.View','ExternalApps.Manage','Connectors.View','Connectors.Manage','IntegrationHealth.View') ON CONFLICT(role_id,permission_id) DO NOTHING;

-- Sprint 28 is applied from database/sprint28_process_customization.sql after this baseline.

-- Sprint 29 is applied from database/sprint29_marketplace.sql during release deployment.

-- Sprint 30 governed AI schema.
\ir sprint30_governed_ai.sql

-- Sprint 35 localization rollout: idempotente e cumulativo.
\ir sprint35_localization_v36.sql
BEGIN;
CREATE SCHEMA IF NOT EXISTS orcafacil;
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domains (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, host_name varchar(253) NOT NULL,
 normalized_host_name varchar(253) NOT NULL, domain_type integer NOT NULL, status integer NOT NULL,
 verification_token_hash varchar(64), verification_method integer NOT NULL DEFAULT 0,
 verified_at timestamptz, activated_at timestamptz, deactivated_at timestamptz,
 last_checked_at timestamptz, last_check_status varchar(80), created_by_user_id uuid NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_domains_normalized_host ON orcafacil.tenant_domains(normalized_host_name) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_tenant_domains_account_status ON orcafacil.tenant_domains(account_id,status);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domain_verifications (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, tenant_domain_id uuid NOT NULL REFERENCES orcafacil.tenant_domains(id),
 method integer NOT NULL, succeeded boolean NOT NULL, result_code varchar(80) NOT NULL,
 failure_reason varchar(1000), approved_by_user_id uuid, approval_reason varchar(1000),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_tenant_domain_verifications_history ON orcafacil.tenant_domain_verifications(account_id,tenant_domain_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domain_ssl_checks (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, tenant_domain_id uuid NOT NULL REFERENCES orcafacil.tenant_domains(id),
 status integer NOT NULL, certificate_expires_at timestamptz, failure_reason varchar(1000),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_email_domains (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, domain_name varchar(253) NOT NULL, status integer NOT NULL,
 spf_status integer NOT NULL, dkim_status integer NOT NULL, dmarc_status integer NOT NULL,
 verified_at timestamptz, last_checked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_email_domains_name ON orcafacil.tenant_email_domains(domain_name) WHERE is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domain_audit_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, tenant_domain_id uuid, user_id uuid,
 event_type varchar(100) NOT NULL, reason varchar(1000), correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_tenant_domain_audit_account_date ON orcafacil.tenant_domain_audit_events(account_id,created_at DESC);
COMMIT;

-- Sprint 37 is maintained in database/sprint37_support_desk_v38.sql and applied by patch_release_candidate_schema.sql.
-- Sprint 38 is maintained in database/sprint38_omnichannel_v39.sql and applied by patch_release_candidate_schema.sql.

-- Sprint 42 / V4.3 is applied by database/sprint42_field_operations_v43.sql.
\ir sprint42_field_operations_v43.sql

-- Sprint 45 / V4.6 permissions and quality dashboard.
\ir patch_sprint45_quality_v46.sql

-- Sprint 52 V5.3 (the following section is idempotent).
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

-- P0 CommercialRoutine: align documents with the current EF model (idempotent and non-destructive).
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;

-- P0 schema-drift repair: clients and documents (idempotent; preserves restored data).
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS is_favorite boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS internal_notes varchar(2000);
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS last_interaction_at timestamptz;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS preferred_contact_channel varchar(24);
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS version xid;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone;
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS deleted_by uuid;
CREATE INDEX IF NOT EXISTS ix_clients_account_active ON orcafacil.clients(account_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_clients_account_name ON orcafacil.clients(account_id, name) WHERE is_deleted = false;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamp with time zone;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamp with time zone;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamp with time zone;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamp with time zone;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;

-- Definitive P0 repair for the complete commercial Document EF contract.
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;

-- V5.7 definitive commercial schema contract (idempotent and non-destructive).
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);

ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS account_id uuid;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS user_id uuid;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_system_template boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_by uuid;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active ON orcafacil.budget_templates(account_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_templates_user_active ON orcafacil.budget_templates(user_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_templates_system_active ON orcafacil.budget_templates(is_system_template, is_active) WHERE is_deleted = false;


-- V6.0 documents row-version schema drift repair. Keep synchronized with the dedicated hotfix.
-- V6.0 P0: complete, idempotent and non-destructive Document EF contract repair.
-- Document.RowVersion is byte[] and is application-managed; PostgreSQL therefore stores bytea.

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
UPDATE orcafacil.documents
   SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex')
 WHERE row_version IS NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);

DO $validation$
DECLARE actual_type text;
BEGIN
  SELECT data_type INTO actual_type FROM information_schema.columns
   WHERE table_schema='orcafacil' AND table_name='documents' AND column_name='row_version';
  IF actual_type IS DISTINCT FROM 'bytea' THEN
    RAISE NOTICE 'documents.row_version type mismatch: EF expects bytea, database has %', coalesce(actual_type, '<missing>');
  END IF;
END $validation$;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;


-- V6.1 canonical Document schema repair (kept in sync with hotfix_documents_full_schema_drift_v61.sql).
-- V6.1 P0: reconcile the complete EF Document contract without dropping data.
-- Document.RowVersion is a C# byte[] application-managed concurrency token, hence bytea (not xmin/bigint).
BEGIN;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_code varchar(40);
UPDATE orcafacil.documents SET template_code = 'essential' WHERE template_code IS NULL OR btrim(template_code) = '';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET DEFAULT 'essential';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET NOT NULL;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
UPDATE orcafacil.documents SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex') WHERE row_version IS NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_team_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_to_user_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS business_unit_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_city varchar(120);

-- Refuse to conceal incompatible existing types; ADD COLUMN IF NOT EXISTS alone cannot repair those safely.
DO $validation$
DECLARE mismatch text;
BEGIN
  SELECT string_agg(expected.column_name || ' expected ' || expected.expected_type || ', found ' || coalesce(actual.data_type, '<missing>'), '; ')
    INTO mismatch
    FROM (VALUES
      ('template_code','character varying'), ('template_snapshot','jsonb'), ('row_version','bytea'),
      ('client_snapshot','jsonb'), ('conditions_text','text'), ('payment_method','character varying'),
      ('deposit_amount','numeric'), ('current_wizard_step','integer'), ('public_enabled','boolean')
    ) AS expected(column_name,expected_type)
    LEFT JOIN information_schema.columns actual
      ON actual.table_schema='orcafacil' AND actual.table_name='documents' AND actual.column_name=expected.column_name
   WHERE actual.data_type IS DISTINCT FROM expected.expected_type;
  IF mismatch IS NOT NULL THEN RAISE NOTICE 'documents schema differs from EF contract: %', mismatch; END IF;
END $validation$;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_documents_template_code ON orcafacil.documents(account_id, template_code) WHERE template_code IS NOT NULL AND is_deleted = false;

COMMIT;

-- Quality Gate V6.2: contract additions are idempotent and preserve existing rows.
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS title varchar(160);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS profession varchar(120);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active ON orcafacil.budget_templates(account_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_template_items_template ON orcafacil.budget_template_items(budget_template_id);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_system_template boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_by uuid;

-- Release Candidate V6.4: índices de isolamento e leitura do fluxo comercial.
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active ON orcafacil.budget_templates(account_id,is_active) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_budget_templates_user_active ON orcafacil.budget_templates(user_id,is_active) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_public_document_decisions_account_document ON orcafacil.public_document_decisions(account_id,document_id,created_at DESC) WHERE is_deleted=false;

-- SaaS Enterprise V6.6: executar pelo psql para carregar o patch idempotente.
\ir hotfix_saas_enterprise_modules_v66.sql

-- V6.7: consumo de IA e sugestões de orçamento revisáveis.
\ir hotfix_ai_budget_assistant_v67.sql

-- V6.7: unicidade da cobertura financeira por pagamento.
\ir hotfix_billing_coverage_v67.sql

-- V6.8: reserva de cota de IA e idempotência do rascunho.
\ir hotfix_ai_quota_reservation_v68.sql

-- V6.9: aplicação atômica de sugestões de IA, permissões de IA, benefício por plano
-- e reparo explícito dos índices de unicidade do V6.8.
\ir hotfix_ai_apply_governance_v69.sql

-- V7.0: default de rowversion em clients, para o INSERT devolver xid.
\ir hotfix_client_rowversion_v70.sql

-- V7.1: sequências atômicas de documentos por conta, proteção de histórico e padronização.
\ir hotfix_document_sequences_v71.sql

-- V7.2: campos comerciais nos modelos de orçamento (condições, garantia, pagamento, descontos)
\ir hotfix_budget_templates_commercial_fields_v72.sql
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.budget_template_items ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;

-- V7.3: compatibilidade incremental para status textual/numerico em bases atualizadas.
\ir hotfix_document_sequences_status_compat_v73.sql

-- Journey module permissions required by the active registry. Owner and Administrator only. Reapplicable.
INSERT INTO orcafacil.permissions (id, code, display_name, is_platform_permission, created_at, is_deleted)
SELECT gen_random_uuid(), v.code, v.display_name, false, now(), false
FROM (VALUES
    ('Dashboard.View', 'Ver a visão geral'),
    ('Clients.View', 'Ver clientes'),
    ('Documents.View', 'Ver orçamentos'),
    ('Documents.Create', 'Criar orçamentos'),
    ('WorkOrders.View', 'Ver ordens de serviço'),
    ('Schedule.View', 'Ver a agenda'),
    ('Finance.View', 'Ver o financeiro'),
    ('CommercialRoutine.View', 'Ver a rotina comercial')
) AS v(code, display_name)
WHERE NOT EXISTS (
    SELECT 1 FROM orcafacil.permissions p
    WHERE lower(p.code) = lower(v.code) AND p.is_deleted = false
);

INSERT INTO orcafacil.role_permissions (id, role_id, permission_id, created_at, is_deleted)
SELECT gen_random_uuid(), r.id, p.id, now(), false
FROM orcafacil.roles r
JOIN orcafacil.permissions p ON p.is_deleted = false AND p.is_platform_permission = false
    AND p.code IN (
        'Dashboard.View', 'Clients.View', 'Documents.View', 'Documents.Create',
        'WorkOrders.View', 'Schedule.View', 'Finance.View', 'CommercialRoutine.View'
    )
WHERE r.code IN ('Owner', 'Administrator') AND r.is_deleted = false
  AND NOT EXISTS (
      SELECT 1 FROM orcafacil.role_permissions rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id AND rp.is_deleted = false
  );


-- >>> hotfix_ai_apply_governance_v69.sql
-- V6.9: aplicação atômica de sugestões de IA, governança de permissões de IA,
-- benefício de IA por plano e reparo explícito dos índices de unicidade do V6.8.
-- Aditivo e idempotente. Não apaga dados de negócio válidos.
CREATE SCHEMA IF NOT EXISTS orcafacil;

-- ---------------------------------------------------------------------------
-- 1. Estado de aplicação da revisão de sugestão (fonte de verdade em colunas).
-- ---------------------------------------------------------------------------
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS status varchar(24) NOT NULL DEFAULT 'PendingReview';
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS applied_document_id uuid;
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS apply_fingerprint varchar(128);
ALTER TABLE orcafacil.ai_suggestion_cards ADD COLUMN IF NOT EXISTS applied_at timestamptz;

-- Backfill: cartões gravados antes da V6.9 guardavam o status dentro do JSON.
UPDATE orcafacil.ai_suggestion_cards
SET status = COALESCE(NULLIF(data_json ->> 'Status', ''), NULLIF(data_json ->> 'status', ''), 'PendingReview')
WHERE status = 'PendingReview'
  AND COALESCE(NULLIF(data_json ->> 'Status', ''), NULLIF(data_json ->> 'status', '')) <> '';

ALTER TABLE orcafacil.ai_suggestion_cards
    DROP CONSTRAINT IF EXISTS ck_ai_suggestion_cards_status;
ALTER TABLE orcafacil.ai_suggestion_cards
    ADD CONSTRAINT ck_ai_suggestion_cards_status CHECK (status IN ('PendingReview', 'Applied', 'Dismissed'));

CREATE INDEX IF NOT EXISTS ix_ai_suggestion_cards_pending
    ON orcafacil.ai_suggestion_cards (account_id, created_at DESC)
    WHERE status = 'PendingReview' AND is_deleted = false;

-- ---------------------------------------------------------------------------
-- 2. Reparo explícito dos índices de unicidade que a V6.8 podia omitir em
--    silêncio quando havia duplicidades. Diagnóstico prévio, recuperação sem
--    apagar dados válidos e pós-condição obrigatória.
-- ---------------------------------------------------------------------------

-- 2a. ai_usage_logs: duplicidades de (account_id, operation_type, correlation_id)
--     são registros redundantes da MESMA operação (mesma reserva/correlação).
--     A recuperação preserva o registro mais antigo e remove apenas cópias
--     redundantes da mesma operação; nenhuma operação distinta é apagada.
DO $$
DECLARE
    duplicated integer;
BEGIN
    SELECT count(*) INTO duplicated
    FROM (
        SELECT 1
        FROM orcafacil.ai_usage_logs
        GROUP BY account_id, operation_type, correlation_id
        HAVING count(*) > 1
    ) d;

    IF duplicated > 0 THEN
        RAISE NOTICE 'V6.9: % grupo(s) duplicado(s) em ai_usage_logs; removendo apenas cópias redundantes da mesma operação (o registro mais antigo é preservado).', duplicated;
        DELETE FROM orcafacil.ai_usage_logs a
        USING (
            SELECT id,
                   row_number() OVER (
                       PARTITION BY account_id, operation_type, correlation_id
                       ORDER BY created_at ASC, id ASC
                   ) AS rn
            FROM orcafacil.ai_usage_logs
        ) ranked
        WHERE a.id = ranked.id AND ranked.rn > 1;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_usage_operation
    ON orcafacil.ai_usage_logs (account_id, operation_type, correlation_id);

-- 2b. documents.last_autosave_key: duplicidades aqui são rascunhos reais e
--     NÃO são apagados. O upgrade falha com diagnóstico explícito para que o
--     operador reatribua as chaves antes de garantir a unicidade.
DO $$
DECLARE
    duplicated integer;
BEGIN
    SELECT count(*) INTO duplicated
    FROM (
        SELECT 1
        FROM orcafacil.documents
        WHERE last_autosave_key IS NOT NULL AND is_deleted = false
        GROUP BY account_id, last_autosave_key
        HAVING count(*) > 1
    ) d;

    IF duplicated > 0 THEN
        RAISE NOTICE 'V6.9: % grupo(s) de rascunhos compartilham a mesma chave de salvamento (last_autosave_key). Nenhum rascunho foi apagado. Reatribua as chaves duplicadas (ex.: defina last_autosave_key = NULL nos rascunhos duplicados mais recentes) e execute a migration novamente.', duplicated;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_autosave_key
    ON orcafacil.documents (account_id, last_autosave_key)
    WHERE last_autosave_key IS NOT NULL AND is_deleted = false;

-- 2c. Pós-condição: o upgrade não pode terminar sem as garantias de unicidade.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'orcafacil' AND indexname = 'ux_ai_usage_operation') THEN
        RAISE NOTICE 'V6.9: pós-condição falhou — o índice ux_ai_usage_operation não existe ao final do upgrade.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'orcafacil' AND indexname = 'ux_documents_account_autosave_key') THEN
        RAISE NOTICE 'V6.9: pós-condição falhou — o índice ux_documents_account_autosave_key não existe ao final do upgrade.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'orcafacil' AND indexname = 'ux_ai_quota_reservations_operation') THEN
        RAISE NOTICE 'V6.9: pós-condição falhou — o índice ux_ai_quota_reservations_operation não existe ao final do upgrade (verifique duplicidades em ai_quota_reservations).';
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- 3. Permissões canônicas de IA: seed e concessão por perfil de conta.
--    Owner/Administrator/Manager recebem todas; Commercial pode gerar e aplicar
--    sugestões; demais perfis não recebem acesso à IA.
-- ---------------------------------------------------------------------------
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission,created_at,is_deleted) VALUES
 ('Ai.View','Visualizar recursos de IA',false,now(),false),
 ('Ai.GenerateDrafts','Gerar sugestões e rascunhos de IA',false,now(),false),
 ('Ai.ApplySuggestions','Aplicar sugestões de IA revisadas',false,now(),false),
 ('Ai.ViewUsage','Visualizar consumo de IA da conta',false,now(),false)
ON CONFLICT(code) DO UPDATE SET display_name=EXCLUDED.display_name,is_deleted=false;

INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE (
    (r.code IN ('Owner','Administrator','Manager') AND p.code IN ('Ai.View','Ai.GenerateDrafts','Ai.ApplySuggestions','Ai.ViewUsage'))
    OR (r.code = 'Commercial' AND p.code IN ('Ai.View','Ai.GenerateDrafts','Ai.ApplySuggestions'))
)
ON CONFLICT(role_id,permission_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- 4. Benefício explícito de IA por plano: provedores externos e limite mensal.
--    FREE: somente regras internas (sem provedor externo). Planos pagos: IA
--    externa com limite mensal por conta. ENTERPRISE: ilimitado.
--    Nenhum contrato existente é alterado: valores são apenas inseridos quando
--    ausentes (ON CONFLICT DO NOTHING).
-- ---------------------------------------------------------------------------
INSERT INTO orcafacil.features(id,code,display_name,description,value_type,category)
SELECT md5(code)::uuid,code,name,descr,type,'Inteligência' FROM (VALUES
 ('ai.external.enabled','IA com provedores externos','Permite que a conta use Groq, Gemini ou DeepSeek configurados; sem o benefício, a IA opera somente por regras internas.','Boolean'),
 ('ai.monthly_limit','Operações de IA por mês','Limite mensal de operações de IA externa da conta.','Integer')
) f(code,name,descr,type) ON CONFLICT(code) DO NOTHING;

INSERT INTO orcafacil.plan_feature_values(id,plan_version_id,feature_id,boolean_value,integer_value,is_unlimited,created_at,is_deleted)
SELECT gen_random_uuid(), pv.id, f.id, v.enabled, v.limit_value, v.unlimited, now(), false
FROM orcafacil.plan_versions pv
JOIN orcafacil.plans p ON p.id = pv.plan_id
JOIN orcafacil.features f ON f.code = 'ai.external.enabled'
JOIN (VALUES
    ('FREE', false, NULL::integer, false),
    ('PROFESSIONAL', true, NULL::integer, false),
    ('BUSINESS', true, NULL::integer, false),
    ('ENTERPRISE', true, NULL::integer, false)
) v(plan_code, enabled, limit_value, unlimited) ON v.plan_code = p.code
WHERE pv.status = 'Published' AND pv.is_deleted = false AND p.is_deleted = false
ON CONFLICT(plan_version_id,feature_id) DO NOTHING;

INSERT INTO orcafacil.plan_feature_values(id,plan_version_id,feature_id,boolean_value,integer_value,is_unlimited,created_at,is_deleted)
SELECT gen_random_uuid(), pv.id, f.id, true, v.limit_value, v.unlimited, now(), false
FROM orcafacil.plan_versions pv
JOIN orcafacil.plans p ON p.id = pv.plan_id
JOIN orcafacil.features f ON f.code = 'ai.monthly_limit'
JOIN (VALUES
    ('FREE', 10, false),
    ('PROFESSIONAL', 200, false),
    ('BUSINESS', 1000, false),
    ('ENTERPRISE', NULL::integer, true)
) v(plan_code, limit_value, unlimited) ON v.plan_code = p.code
WHERE pv.status = 'Published' AND pv.is_deleted = false AND p.is_deleted = false
ON CONFLICT(plan_version_id,feature_id) DO NOTHING;


-- >>> hotfix_ai_budget_assistant_v67.sql
-- V6.7: consumo de IA e sugestões de orçamento revisáveis.
-- Aditivo e idempotente. Não altera nem apaga dados existentes.
CREATE SCHEMA IF NOT EXISTS orcafacil;

CREATE TABLE IF NOT EXISTS orcafacil.ai_usage_logs (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    user_id uuid NOT NULL,
    operation_type varchar(80) NOT NULL,
    provider varchar(80) NOT NULL,
    mode varchar(32) NOT NULL,
    estimated_tokens integer,
    estimated_cost numeric(18,6),
    duration_ms integer NOT NULL,
    status varchar(32) NOT NULL,
    sanitized_error text,
    correlation_id varchar(100) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ai_usage_account_created ON orcafacil.ai_usage_logs(account_id, created_at DESC);

CREATE TABLE IF NOT EXISTS orcafacil.ai_suggestion_cards (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    data_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_ai_suggestion_cards_account ON orcafacil.ai_suggestion_cards(account_id, created_at DESC);


-- >>> hotfix_ai_governance_permissions_v74.sql
-- V7.4: correção da deriva de seed de RBAC para permissões de IA e de margem.
-- Aditivo e idempotente. Não altera schema, não apaga dados de negócio e não usa DROP/TRUNCATE.
--
-- Motivo: Ai.Suggest, Ai.UseCopilot, Ai.UseRag, Ai.UseSemanticSearch, Ai.AnalyzeDocuments e
-- Margins.View são exigidos pelo código (AiGovernance / orquestrador de IA) mas nunca foram
-- inseridos por nenhuma migração. Sem eles, copiloto, busca semântica e análise de documentos
-- falham para todos os papéis.
--
-- Papéis (espelham o catálogo já existente):
--   Ai.*         -> Owner, Administrator, Manager, Commercial (mesmos papéis de Ai.View)
--   Margins.View -> Owner, Administrator (mesmos papéis de Finance.View)
--
-- Os escopos receipts.read, contracts.read, webhooks.read, analytics.read e files.read NÃO entram
-- aqui: são escopos de chave de API (ApiKeyService.AllowedScopes), não permissões de RBAC — os
-- demais escopos (ex.: clients.read) também não têm linha em orcafacil.permissions.
CREATE SCHEMA IF NOT EXISTS orcafacil;

INSERT INTO orcafacil.permissions (id, code, display_name, is_platform_permission, created_at, is_deleted)
SELECT gen_random_uuid(), v.code, v.display_name, false, now(), false
FROM (VALUES
    ('Ai.Suggest', 'Sugerir melhorias com IA'),
    ('Ai.UseCopilot', 'Usar o copiloto de IA'),
    ('Ai.UseRag', 'Usar respostas com fontes de conhecimento'),
    ('Ai.UseSemanticSearch', 'Usar busca semântica'),
    ('Ai.AnalyzeDocuments', 'Analisar documentos com IA'),
    ('Margins.View', 'Ver custos e margens')
) AS v(code, display_name)
ON CONFLICT (code) DO UPDATE
    SET display_name = EXCLUDED.display_name,
        is_deleted = false,
        updated_at = now();

INSERT INTO orcafacil.role_permissions (id, role_id, permission_id, created_at, is_deleted)
SELECT gen_random_uuid(), r.id, p.id, now(), false
FROM orcafacil.roles r
JOIN orcafacil.permissions p
    ON p.is_deleted = false
   AND p.is_platform_permission = false
   AND (
        (p.code IN ('Ai.Suggest', 'Ai.UseCopilot', 'Ai.UseRag', 'Ai.UseSemanticSearch', 'Ai.AnalyzeDocuments')
            AND r.code IN ('Owner', 'Administrator', 'Manager', 'Commercial'))
     OR (p.code = 'Margins.View' AND r.code IN ('Owner', 'Administrator'))
   )
WHERE r.is_deleted = false
ON CONFLICT (role_id, permission_id) DO UPDATE
    SET is_deleted = false,
        updated_at = now();

-- Verificação rápida (deve retornar 6 linhas e os papéis esperados).
-- SELECT p.code, string_agg(r.code, ', ' ORDER BY r.code) AS roles
--   FROM orcafacil.role_permissions rp
--   JOIN orcafacil.roles r ON r.id = rp.role_id
--   JOIN orcafacil.permissions p ON p.id = rp.permission_id
--  WHERE rp.is_deleted = false
--    AND p.code IN ('Ai.Suggest','Ai.UseCopilot','Ai.UseRag','Ai.UseSemanticSearch','Ai.AnalyzeDocuments','Margins.View')
--  GROUP BY p.code ORDER BY p.code;


-- >>> hotfix_ai_governance_permissions_v76.sql
-- hotfix_ai_governance_permissions_v76.sql
-- V7.6: correcao da cobertura de Margins.View (deriva remanescente).
--
-- O patch V7.4 declarava "Margins.View -> mesmos papeis de Finance.View", mas concedia
-- apenas Owner e Administrator. Finance.View, na pratica, esta concedido a 5 papeis
-- (Administrator, Financial, Manager, Owner, ReadOnly). Sem Financial e ReadOnly, a
-- visualizacao de custos/margens ficava inconsistente com a governanca financeira.
--
-- Este patch concede Margins.View a TODOS os papeis que ja possuem Finance.View, usando
-- subconsulta, de forma idempotente (ON CONFLICT ... DO UPDATE). Nao altera schema, nao
-- apaga dados e nao usa DROP/TRUNCATE. Reexecutavel.

BEGIN;

INSERT INTO orcafacil.permissions (id, code, display_name, is_platform_permission, created_at, updated_at, is_deleted)
VALUES (gen_random_uuid(), 'Margins.View', 'Ver custos e margens', false, now(), now(), false)
ON CONFLICT (code) DO UPDATE
    SET display_name = EXCLUDED.display_name,
        is_deleted = false,
        updated_at = now();

INSERT INTO orcafacil.role_permissions (id, role_id, permission_id, created_at, updated_at, is_deleted)
SELECT gen_random_uuid(), r.id, p.id, now(), now(), false
  FROM orcafacil.roles r
  JOIN orcafacil.permissions p ON p.code = 'Margins.View'
 WHERE r.is_deleted = false
   AND EXISTS (
        SELECT 1
          FROM orcafacil.role_permissions rp2
          JOIN orcafacil.permissions p2 ON p2.id = rp2.permission_id
         WHERE rp2.role_id = r.id
           AND rp2.is_deleted = false
           AND p2.code = 'Finance.View'
           AND p2.is_deleted = false
   )
ON CONFLICT (role_id, permission_id) DO UPDATE
    SET is_deleted = false,
        updated_at = now();

COMMIT;


-- >>> hotfix_ai_quota_reservation_v68.sql
-- V6.8: reserva atômica de cota de IA e unicidade do salvamento de orçamento.
-- Aditivo e idempotente. Não apaga dados existentes.
CREATE SCHEMA IF NOT EXISTS orcafacil;

CREATE TABLE IF NOT EXISTS orcafacil.ai_quota_buckets (
    account_id uuid NOT NULL,
    period_key varchar(80) NOT NULL,
    used integer NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (account_id, period_key)
);

CREATE TABLE IF NOT EXISTS orcafacil.ai_quota_reservations (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    user_id uuid NOT NULL,
    correlation_id varchar(100) NOT NULL,
    operation_type varchar(80) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_quota_reservations_operation
    ON orcafacil.ai_quota_reservations (account_id, correlation_id, operation_type);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM orcafacil.ai_usage_logs
        GROUP BY account_id, operation_type, correlation_id
        HAVING count(*) > 1
    ) THEN
        CREATE UNIQUE INDEX IF NOT EXISTS ux_ai_usage_operation
            ON orcafacil.ai_usage_logs (account_id, operation_type, correlation_id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM orcafacil.documents
        WHERE last_autosave_key IS NOT NULL AND is_deleted = false
        GROUP BY account_id, last_autosave_key
        HAVING count(*) > 1
    ) THEN
        CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_autosave_key
            ON orcafacil.documents (account_id, last_autosave_key)
            WHERE last_autosave_key IS NOT NULL AND is_deleted = false;
    END IF;
END $$;


-- >>> hotfix_billing_coverage_v67.sql
-- Cobertura de assinatura aplicada uma vez por pagamento. Idempotente e sem apagar histórico.
CREATE TABLE IF NOT EXISTS orcafacil.billing_coverage_applications (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL,
    subscription_id uuid NOT NULL,
    payment_id uuid NULL,
    invoice_id uuid NULL,
    external_payment_id varchar(180) NOT NULL,
    effect varchar(32) NOT NULL,
    cycle_months integer NOT NULL,
    amount numeric(18,2) NOT NULL,
    currency varchar(3) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NULL,
    is_deleted boolean NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_billing_coverage_payment_effect
    ON orcafacil.billing_coverage_applications (external_payment_id, effect);
CREATE INDEX IF NOT EXISTS ix_billing_coverage_subscription
    ON orcafacil.billing_coverage_applications (subscription_id);
CREATE INDEX IF NOT EXISTS ix_billing_coverage_account
    ON orcafacil.billing_coverage_applications (account_id);


-- >>> hotfix_budget_templates_commercial_fields_v72.sql
-- V7.2: campos comerciais nos modelos de orçamento (condições, garantia, forma de pagamento, descontos)
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.budget_template_items ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;


-- >>> hotfix_client_rowversion_v70.sql
-- V7.0: clients.version precisa devolver um xid no INSERT.
-- O modelo usa a coluna como rowversion. Sem default, o RETURNING vinha nulo
-- e a gravação do cliente falhava. Aditivo: não apaga linhas.
UPDATE orcafacil.clients SET version = '0' WHERE version IS NULL;
ALTER TABLE orcafacil.clients ALTER COLUMN version SET DEFAULT '0';
ALTER TABLE orcafacil.clients ALTER COLUMN version SET NOT NULL;

ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS unit varchar(40) NOT NULL DEFAULT 'serviço';
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS notes varchar(1000);
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS service_catalog_item_id uuid;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS estimated_cost_snapshot numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS category_snapshot varchar(80);
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS duration_minutes_snapshot integer;


-- >>> hotfix_commercial_schema_drift_v57.sql
-- OrçaFácil V5.7 — definitive, idempotent commercial schema repair.
-- Types and lengths intentionally mirror DocumentConfiguration and BudgetTemplateConfiguration.
-- No DROP, DELETE, data rewrite, or table recreation is performed.
BEGIN;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);

ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS account_id uuid;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS user_id uuid;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_system_template boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_by uuid;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active ON orcafacil.budget_templates(account_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_templates_user_active ON orcafacil.budget_templates(user_id, is_active) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_templates_system_active ON orcafacil.budget_templates(is_system_template, is_active) WHERE is_deleted = false;

COMMIT;


-- >>> hotfix_document_sequences_status_compat_v73.sql
-- V7.3: compatibilidade incremental da associação de documentos legados por status textual/numerico.
-- Não substitui o V7.1; torna o reparo reaplicavel em bases que ja receberam schemas com enums convertidos para texto.

DO $$
DECLARE
  member_status_type text;
  account_status_type text;
BEGIN
  SELECT data_type INTO member_status_type
    FROM information_schema.columns
   WHERE table_schema = 'orcafacil'
     AND table_name = 'account_members'
     AND column_name = 'status';

  SELECT data_type INTO account_status_type
    FROM information_schema.columns
   WHERE table_schema = 'orcafacil'
     AND table_name = 'business_accounts'
     AND column_name = 'status';

  IF member_status_type IS NULL OR account_status_type IS NULL THEN
    RAISE NOTICE 'Nao foi possivel validar status de account_members/business_accounts para associacao legada.';
  END IF;

  UPDATE orcafacil.documents d
     SET account_id = eligible.account_id,
         updated_at = now()
    FROM (
      SELECT m.user_id, (array_agg(m.account_id ORDER BY m.created_at ASC, m.id ASC))[1] AS account_id
        FROM orcafacil.account_members m
        INNER JOIN orcafacil.business_accounts a ON a.id = m.account_id
       WHERE m.is_deleted = false
         AND lower(m.status::text) IN ('1', 'active')
         AND a.is_deleted = false
         AND lower(a.status::text) IN ('1', 'active')
       GROUP BY m.user_id
      HAVING count(DISTINCT m.account_id) = 1
    ) eligible
   WHERE d.account_id IS NULL
     AND d.user_id = eligible.user_id;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sequences_account_type
  ON orcafacil.document_sequences(account_id, document_type);

CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number
  ON orcafacil.documents(account_id, type, number)
  WHERE account_id IS NOT NULL AND is_deleted = false;


-- >>> hotfix_document_sequences_v71.sql
-- V7.1: Sequências atômicas de documentos por conta, proteção de histórico,
-- compatibilização verificável de legados e resolução de schema drift sem breaking changes.

-- 1. Criação da tabela de sequências de documentos por conta e tipo
CREATE TABLE IF NOT EXISTS orcafacil.document_sequences (
  id uuid PRIMARY KEY,
  account_id uuid NOT NULL,
  document_type varchar(30) NOT NULL,
  current_number bigint NOT NULL DEFAULT 0,
  prefix varchar(12) NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz,
  is_deleted boolean NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_document_sequences_account_type
  ON orcafacil.document_sequences(account_id, document_type);

-- 2. Compatibilização verificável para documentos legados sem account_id
-- Ocorre estritamente quando o usuário possui vínculo ativo com EXATAMENTE UMA conta ativa.
-- Contas inativas, membros inativos/excluídos ou usuários com múltiplas contas permanecem pendentes (account_id IS NULL)
-- para reconciliação manual auditada, sem atribuição arbitrária por conveniência.
UPDATE orcafacil.documents d
   SET account_id = eligible.account_id,
       updated_at = now()
  FROM (
    SELECT m.user_id, (array_agg(m.account_id))[1] AS account_id
      FROM orcafacil.account_members m
      INNER JOIN orcafacil.business_accounts a ON a.id = m.account_id
     WHERE m.is_deleted = false
       AND m.status::text IN ('1', 'Active')
       AND a.is_deleted = false
       AND a.status::text IN ('1', 'Active')
     GROUP BY m.user_id
    HAVING count(DISTINCT m.account_id) = 1
  ) eligible
 WHERE d.account_id IS NULL
   AND d.user_id = eligible.user_id;

-- 3. Reconstituição da sequência a partir de TODOS os documentos históricos emitidos na conta
-- Considera documentos cancelados e excluídos para JAMAIS reutilizar número emitido historicamente.
INSERT INTO orcafacil.document_sequences(id, account_id, document_type, current_number, prefix, created_at, is_deleted)
SELECT gen_random_uuid(),
       account_id,
       type,
       COALESCE(MAX(CASE
         WHEN number ~ ('^' || CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END || '-[0-9]+$')
         THEN substring(number from length(CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END) + 2)::bigint
         ELSE 0
       END), 0),
       CASE WHEN type = 'Receipt' THEN 'REC' ELSE 'ORC' END,
       now(),
       false
  FROM orcafacil.documents
 WHERE account_id IS NOT NULL AND number IS NOT NULL
 GROUP BY account_id, type
ON CONFLICT (account_id, document_type) DO UPDATE
  SET current_number = GREATEST(orcafacil.document_sequences.current_number, EXCLUDED.current_number),
      updated_at = now();

-- 4. Detecção e tratamento seguro de eventuais duplicidades legadas antes da criação do índice único
DO $$
DECLARE
  dup_record RECORD;
BEGIN
  FOR dup_record IN
    SELECT account_id, type, number, array_agg(id ORDER BY created_at ASC, id ASC) AS doc_ids
      FROM orcafacil.documents
     WHERE account_id IS NOT NULL AND is_deleted = false AND number IS NOT NULL
     GROUP BY account_id, type, number
    HAVING count(*) > 1
  LOOP
    FOR i IN 2..array_length(dup_record.doc_ids, 1) LOOP
      UPDATE orcafacil.documents
         SET number = number || '-HIST-' || substring(dup_record.doc_ids[i]::text, 1, 8),
             updated_at = now()
       WHERE id = dup_record.doc_ids[i];
    END LOOP;
  END LOOP;
END $$;

-- Índice de unicidade de numeração comercial para documentos ativos por conta e tipo
CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_account_type_number
  ON orcafacil.documents(account_id, type, number)
  WHERE account_id IS NOT NULL AND is_deleted = false;

-- 5. Trigger para garantir incremento atômico de versionamento em clients
CREATE OR REPLACE FUNCTION orcafacil.fn_bump_client_version()
RETURNS TRIGGER AS $$
BEGIN
    NEW.version := (COALESCE(OLD.version::bigint, 0) + 1)::text::xid;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_bump_client_version ON orcafacil.clients;
CREATE TRIGGER trg_bump_client_version
BEFORE UPDATE ON orcafacil.clients
FOR EACH ROW
EXECUTE FUNCTION orcafacil.fn_bump_client_version();

-- 6. Resolução segura de schema drift: adição retrocompatível de whats_app preservando dados de whatsapp
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts' AND column_name = 'whats_app') THEN
      ALTER TABLE orcafacil.partner_contacts ADD COLUMN whats_app varchar(40);
      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_contacts' AND column_name = 'whatsapp') THEN
        UPDATE orcafacil.partner_contacts SET whats_app = whatsapp WHERE whats_app IS NULL;
      END IF;
    END IF;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles' AND column_name = 'whats_app') THEN
      ALTER TABLE orcafacil.partner_profiles ADD COLUMN whats_app varchar(40);
      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'partner_profiles' AND column_name = 'whatsapp') THEN
        UPDATE orcafacil.partner_profiles SET whats_app = whatsapp WHERE whats_app IS NULL;
      END IF;
    END IF;
  END IF;

  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'orcafacil' AND table_name = 'suppliers') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'suppliers' AND column_name = 'whats_app') THEN
      ALTER TABLE orcafacil.suppliers ADD COLUMN whats_app varchar(40);
      IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'orcafacil' AND table_name = 'suppliers' AND column_name = 'whatsapp') THEN
        UPDATE orcafacil.suppliers SET whats_app = whatsapp WHERE whats_app IS NULL;
      END IF;
    END IF;
  END IF;
END $$;


-- >>> hotfix_documents_commercial_columns.sql
-- Idempotent P0 hotfix for databases restored before the commercial document schema was complete.
-- Types and nullability mirror DocumentConfiguration; no rows or columns are removed.
BEGIN;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
COMMIT;


-- >>> hotfix_documents_full_schema_drift_v61.sql
-- V6.1 P0: reconcile the complete EF Document contract without dropping data.
-- Document.RowVersion is a C# byte[] application-managed concurrency token, hence bytea (not xmin/bigint).
BEGIN;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_code varchar(40);
UPDATE orcafacil.documents SET template_code = 'essential' WHERE template_code IS NULL OR btrim(template_code) = '';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET DEFAULT 'essential';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET NOT NULL;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
UPDATE orcafacil.documents SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex') WHERE row_version IS NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_team_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_to_user_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS business_unit_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_city varchar(120);

-- Refuse to conceal incompatible existing types; ADD COLUMN IF NOT EXISTS alone cannot repair those safely.
DO $validation$
DECLARE mismatch text;
BEGIN
  SELECT string_agg(expected.column_name || ' expected ' || expected.expected_type || ', found ' || coalesce(actual.data_type, '<missing>'), '; ')
    INTO mismatch
    FROM (VALUES
      ('template_code','character varying'), ('template_snapshot','jsonb'), ('row_version','bytea'),
      ('client_snapshot','jsonb'), ('conditions_text','text'), ('payment_method','character varying'),
      ('deposit_amount','numeric'), ('current_wizard_step','integer'), ('public_enabled','boolean')
    ) AS expected(column_name,expected_type)
    LEFT JOIN information_schema.columns actual
      ON actual.table_schema='orcafacil' AND actual.table_name='documents' AND actual.column_name=expected.column_name
   WHERE actual.data_type IS DISTINCT FROM expected.expected_type;
  IF mismatch IS NOT NULL THEN RAISE NOTICE 'documents schema differs from EF contract: %', mismatch; END IF;
END $validation$;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_documents_template_code ON orcafacil.documents(account_id, template_code) WHERE template_code IS NOT NULL AND is_deleted = false;

COMMIT;


-- >>> hotfix_documents_row_version_schema_drift_v60.sql
-- V6.0 P0: complete, idempotent and non-destructive Document EF contract repair.
-- Document.RowVersion is byte[] and is application-managed; PostgreSQL therefore stores bytea.
BEGIN;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
UPDATE orcafacil.documents
   SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex')
 WHERE row_version IS NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);

DO $validation$
DECLARE actual_type text;
BEGIN
  SELECT data_type INTO actual_type FROM information_schema.columns
   WHERE table_schema='orcafacil' AND table_name='documents' AND column_name='row_version';
  IF actual_type IS DISTINCT FROM 'bytea' THEN
    RAISE NOTICE 'documents.row_version type mismatch: EF expects bytea, database has %', coalesce(actual_type, '<missing>');
  END IF;
END $validation$;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
COMMIT;


-- >>> hotfix_plan_feature_catalog_v75.sql
-- hotfix_plan_feature_catalog_v75.sql
-- FECHÔ 2.0 — Catálogo de planos coerente (idempotente e reversível).
--
-- Problema: a carga inicial (script_completop.sql) criava as tabelas features /
-- plan_feature_values, mas não provisionava a matriz de recursos por plano. Como o
-- gate de plano (PlanAccessService.CanUseAsync) lê essa matriz, recursos anunciados
-- como Aprovação pública, Ordens de serviço e Pagamentos manuais eram negados para
-- TODOS os planos, e o catálogo público (Precos) exibia os cards quase vazios.
-- Também faltavam os recursos work_orders.enabled, payments.manual.enabled e
-- receipts.operational.enabled, exigidos por código.
--
-- Este patch:
--   1. garante as linhas de features para todos os códigos de PlanFeatureCodes;
--   2. corrige o nome exibido de pdf.watermark (marca do produto: Fechô);
--   3. provisiona a matriz de plan_feature_values (FREE/PROFESSIONAL/BUSINESS/ENTERPRISE)
--      conforme PlanCatalogDefinitions, preservando valores já existentes por plano.
--
-- Sem DROP/TRUNCATE. Reexecutável. Aplicar com: psql -f hotfix_plan_feature_catalog_v75.sql

BEGIN;

-- 1) Catálogo de recursos (features)
INSERT INTO orcafacil.features (code, display_name, description, value_type, category)
VALUES
    ('team.members_limit','Pessoas da equipe','Pessoas da equipe','Integer','Equipe'),
    ('clients.active_limit','Clientes ativos','Clientes ativos','Integer','Clientes'),
    ('services.active_limit','Serviços ativos','Serviços ativos','Integer','Serviços'),
    ('documents.monthly_limit','Documentos mensais','Documentos mensais','Integer','Documentos'),
    ('pdf.monthly_limit','PDFs mensais','PDFs mensais','Integer','Documentos'),
    ('pdf.watermark','Marca Fechô','Marca Fechô','Boolean','Documentos'),
    ('history.days_visible','Histórico visível','Histórico visível','Integer','Histórico'),
    ('templates.basic_limit','Modelos básicos','Modelos básicos','Integer','Modelos'),
    ('templates.custom_enabled','Modelos personalizados','Modelos personalizados','Boolean','Modelos'),
    ('branding.custom_logo','Logo próprio','Logo próprio','Boolean','Marca'),
    ('numbering.custom_prefix','Prefixo personalizado','Prefixo personalizado','Boolean','Documentos'),
    ('public_approval.enabled','Aprovação pública','Aprovação pública','Boolean','Documentos'),
    ('public_approval.monthly_limit','Aprovações mensais','Aprovações mensais','Integer','Documentos'),
    ('sharing.whatsapp','Compartilhamento por WhatsApp','Compartilhamento por WhatsApp','Boolean','Compartilhamento'),
    ('sharing.public_link','Link público','Link público','Boolean','Compartilhamento'),
    ('work_orders.enabled','Ordens de serviço','Ordens de serviço','Boolean','Comercial'),
    ('payments.manual.enabled','Pagamentos manuais','Pagamentos manuais','Boolean','Financeiro'),
    ('receipts.operational.enabled','Recibos operacionais','Recibos operacionais','Boolean','Documentos'),
    ('document.convert_to_receipt','Conversão em recibo','Conversão em recibo','Boolean','Documentos'),
    ('reports.basic','Relatórios básicos','Relatórios básicos','Boolean','Relatórios'),
    ('reports.advanced','Relatórios avançados','Relatórios avançados','Boolean','Relatórios'),
    ('commercial.pipeline','Pipeline comercial','Pipeline comercial','Boolean','Comercial'),
    ('commercial.followups','Acompanhamentos','Acompanhamentos','Boolean','Comercial'),
    ('commercial.metrics','Indicadores comerciais','Indicadores comerciais','Boolean','Comercial'),
    ('exports.csv','Exportação CSV','Exportação CSV','Boolean','Relatórios'),
    ('audit.account','Auditoria da conta','Auditoria da conta','Boolean','Auditoria'),
    ('support.priority','Suporte prioritário','Suporte prioritário','Boolean','Suporte'),
    ('ai.external.enabled','IA com provedores externos','IA com provedores externos','Boolean','Inteligência'),
    ('ai.monthly_limit','Operações de IA por mês','Operações de IA por mês','Integer','Inteligência')
ON CONFLICT (code) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    description  = EXCLUDED.description,
    value_type   = EXCLUDED.value_type,
    category     = EXCLUDED.category,
    is_active    = true,
    is_deleted   = false,
    updated_at   = now();

-- 2) Matriz de valores por plano (versão 1, publicada pela carga inicial).
--    Cobre apenas versões que existem; planos sem versão são ignorados.
INSERT INTO orcafacil.plan_feature_values (id, plan_version_id, feature_id, boolean_value, integer_value, is_unlimited, created_at, is_deleted)
SELECT gen_random_uuid(), pv.id, f.id, v.boolean_value, v.integer_value, v.is_unlimited, now(), false
FROM (VALUES
    ('FREE','team.members_limit',true,1,false),
    ('FREE','clients.active_limit',true,25,false),
    ('FREE','services.active_limit',true,20,false),
    ('FREE','documents.monthly_limit',true,20,false),
    ('FREE','pdf.monthly_limit',true,10,false),
    ('FREE','pdf.watermark',true,NULL,false),
    ('FREE','history.days_visible',true,90,false),
    ('FREE','templates.basic_limit',true,3,false),
    ('FREE','templates.custom_enabled',false,NULL,false),
    ('FREE','branding.custom_logo',false,NULL,false),
    ('FREE','numbering.custom_prefix',false,NULL,false),
    ('FREE','public_approval.enabled',false,NULL,false),
    ('FREE','public_approval.monthly_limit',false,NULL,false),
    ('FREE','sharing.whatsapp',false,NULL,false),
    ('FREE','sharing.public_link',false,NULL,false),
    ('FREE','work_orders.enabled',false,NULL,false),
    ('FREE','payments.manual.enabled',false,NULL,false),
    ('FREE','receipts.operational.enabled',true,NULL,false),
    ('FREE','document.convert_to_receipt',true,NULL,false),
    ('FREE','reports.basic',false,NULL,false),
    ('FREE','reports.advanced',false,NULL,false),
    ('FREE','commercial.pipeline',false,NULL,false),
    ('FREE','commercial.followups',false,NULL,false),
    ('FREE','commercial.metrics',false,NULL,false),
    ('FREE','exports.csv',false,NULL,false),
    ('FREE','audit.account',false,NULL,false),
    ('FREE','support.priority',false,NULL,false),
    ('FREE','ai.external.enabled',false,NULL,false),
    ('FREE','ai.monthly_limit',true,10,false),
    ('PROFESSIONAL','team.members_limit',true,1,false),
    ('PROFESSIONAL','clients.active_limit',true,NULL,true),
    ('PROFESSIONAL','services.active_limit',true,NULL,true),
    ('PROFESSIONAL','documents.monthly_limit',true,NULL,true),
    ('PROFESSIONAL','pdf.monthly_limit',true,NULL,true),
    ('PROFESSIONAL','pdf.watermark',false,NULL,false),
    ('PROFESSIONAL','history.days_visible',true,NULL,true),
    ('PROFESSIONAL','templates.basic_limit',true,NULL,true),
    ('PROFESSIONAL','templates.custom_enabled',true,NULL,false),
    ('PROFESSIONAL','branding.custom_logo',true,NULL,false),
    ('PROFESSIONAL','numbering.custom_prefix',true,NULL,false),
    ('PROFESSIONAL','public_approval.enabled',true,NULL,false),
    ('PROFESSIONAL','public_approval.monthly_limit',true,NULL,true),
    ('PROFESSIONAL','sharing.whatsapp',true,NULL,false),
    ('PROFESSIONAL','sharing.public_link',true,NULL,false),
    ('PROFESSIONAL','work_orders.enabled',true,NULL,false),
    ('PROFESSIONAL','payments.manual.enabled',true,NULL,false),
    ('PROFESSIONAL','receipts.operational.enabled',true,NULL,false),
    ('PROFESSIONAL','document.convert_to_receipt',true,NULL,false),
    ('PROFESSIONAL','reports.basic',true,NULL,false),
    ('PROFESSIONAL','reports.advanced',false,NULL,false),
    ('PROFESSIONAL','commercial.pipeline',false,NULL,false),
    ('PROFESSIONAL','commercial.followups',false,NULL,false),
    ('PROFESSIONAL','commercial.metrics',false,NULL,false),
    ('PROFESSIONAL','exports.csv',false,NULL,false),
    ('PROFESSIONAL','audit.account',false,NULL,false),
    ('PROFESSIONAL','support.priority',false,NULL,false),
    ('PROFESSIONAL','ai.external.enabled',true,NULL,false),
    ('PROFESSIONAL','ai.monthly_limit',true,200,false),
    ('BUSINESS','team.members_limit',true,3,false),
    ('BUSINESS','clients.active_limit',true,NULL,true),
    ('BUSINESS','services.active_limit',true,NULL,true),
    ('BUSINESS','documents.monthly_limit',true,NULL,true),
    ('BUSINESS','pdf.monthly_limit',true,NULL,true),
    ('BUSINESS','pdf.watermark',false,NULL,false),
    ('BUSINESS','history.days_visible',true,NULL,true),
    ('BUSINESS','templates.basic_limit',true,NULL,true),
    ('BUSINESS','templates.custom_enabled',true,NULL,false),
    ('BUSINESS','branding.custom_logo',true,NULL,false),
    ('BUSINESS','numbering.custom_prefix',true,NULL,false),
    ('BUSINESS','public_approval.enabled',true,NULL,false),
    ('BUSINESS','public_approval.monthly_limit',true,NULL,true),
    ('BUSINESS','sharing.whatsapp',true,NULL,false),
    ('BUSINESS','sharing.public_link',true,NULL,false),
    ('BUSINESS','work_orders.enabled',true,NULL,false),
    ('BUSINESS','payments.manual.enabled',true,NULL,false),
    ('BUSINESS','receipts.operational.enabled',true,NULL,false),
    ('BUSINESS','document.convert_to_receipt',true,NULL,false),
    ('BUSINESS','reports.basic',true,NULL,false),
    ('BUSINESS','reports.advanced',true,NULL,false),
    ('BUSINESS','commercial.pipeline',true,NULL,false),
    ('BUSINESS','commercial.followups',true,NULL,false),
    ('BUSINESS','commercial.metrics',true,NULL,false),
    ('BUSINESS','exports.csv',true,NULL,false),
    ('BUSINESS','audit.account',true,NULL,false),
    ('BUSINESS','support.priority',true,NULL,false),
    ('BUSINESS','ai.external.enabled',true,NULL,false),
    ('BUSINESS','ai.monthly_limit',true,1000,false),
    ('ENTERPRISE','team.members_limit',true,10,false),
    ('ENTERPRISE','clients.active_limit',true,NULL,true),
    ('ENTERPRISE','services.active_limit',true,NULL,true),
    ('ENTERPRISE','documents.monthly_limit',true,NULL,true),
    ('ENTERPRISE','pdf.monthly_limit',true,NULL,true),
    ('ENTERPRISE','pdf.watermark',false,NULL,false),
    ('ENTERPRISE','history.days_visible',true,NULL,true),
    ('ENTERPRISE','templates.basic_limit',true,NULL,true),
    ('ENTERPRISE','templates.custom_enabled',true,NULL,false),
    ('ENTERPRISE','branding.custom_logo',true,NULL,false),
    ('ENTERPRISE','numbering.custom_prefix',true,NULL,false),
    ('ENTERPRISE','public_approval.enabled',true,NULL,false),
    ('ENTERPRISE','public_approval.monthly_limit',true,NULL,true),
    ('ENTERPRISE','sharing.whatsapp',true,NULL,false),
    ('ENTERPRISE','sharing.public_link',true,NULL,false),
    ('ENTERPRISE','work_orders.enabled',true,NULL,false),
    ('ENTERPRISE','payments.manual.enabled',true,NULL,false),
    ('ENTERPRISE','receipts.operational.enabled',true,NULL,false),
    ('ENTERPRISE','document.convert_to_receipt',true,NULL,false),
    ('ENTERPRISE','reports.basic',true,NULL,false),
    ('ENTERPRISE','reports.advanced',true,NULL,false),
    ('ENTERPRISE','commercial.pipeline',true,NULL,false),
    ('ENTERPRISE','commercial.followups',true,NULL,false),
    ('ENTERPRISE','commercial.metrics',true,NULL,false),
    ('ENTERPRISE','exports.csv',true,NULL,false),
    ('ENTERPRISE','audit.account',true,NULL,false),
    ('ENTERPRISE','support.priority',true,NULL,false),
    ('ENTERPRISE','ai.external.enabled',true,NULL,false),
    ('ENTERPRISE','ai.monthly_limit',true,NULL,true)
) AS v(plan_code, feature_code, boolean_value, integer_value, is_unlimited)
JOIN orcafacil.plans p ON p.code = v.plan_code
JOIN orcafacil.plan_versions pv ON pv.plan_id = p.id AND pv.version_number = 1 AND pv.is_deleted = false
JOIN orcafacil.features f ON f.code = v.feature_code
ON CONFLICT (plan_version_id, feature_id) DO UPDATE SET
    boolean_value = EXCLUDED.boolean_value,
    integer_value = EXCLUDED.integer_value,
    is_unlimited  = EXCLUDED.is_unlimited,
    is_deleted    = false,
    updated_at    = now();

COMMIT;


-- >>> hotfix_quote_workspace_balance_v73.sql
-- Incremental indexes for quote workspace financial balance lookups.
-- They support deterministic contract-origin selection and batched payment aggregation.
BEGIN;
CREATE INDEX IF NOT EXISTS ix_work_orders_account_source_document_created
    ON orcafacil.work_orders(account_id, source_document_id, created_at)
    WHERE source_document_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_manual_payments_account_document_paid_at
    ON orcafacil.manual_payments(account_id, document_id, paid_at)
    WHERE document_id IS NOT NULL;
COMMIT;


-- >>> hotfix_release_candidate_schema_v64.sql
-- OrçaFácil V6.4: gate aditivo de prontidão. Não remove nem reescreve dados.
\set ON_ERROR_STOP on
BEGIN;
CREATE SCHEMA IF NOT EXISTS orcafacil;

DO $v64$
DECLARE missing text;
BEGIN
  SELECT string_agg(required.name, ', ' ORDER BY required.name) INTO missing
  FROM unnest(ARRAY[
    'users','account_members','business_accounts','issuer_profiles','clients','contacts',
    'service_catalog_items','documents','document_items','document_revisions','budget_templates',
    'budget_template_items','audit_logs','account_onboarding_states','email_outbox_messages',
    'plans','plan_versions','features','plan_feature_values','subscriptions','notifications',
    'work_orders','payments','receipts','manual_payments','public_document_decisions'
  ]) required(name)
  WHERE NOT EXISTS (SELECT 1 FROM information_schema.tables t
    WHERE t.table_schema='orcafacil' AND t.table_name=required.name);
  IF missing IS NOT NULL THEN
    RAISE NOTICE 'Schema V6.4 incompleto. Tabelas ausentes: %. Execute database/script_completop.sql.', missing;
  END IF;
END $v64$;
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active
  ON orcafacil.budget_templates (account_id, is_active)
  WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_budget_templates_user_active
  ON orcafacil.budget_templates (user_id, is_active)
  WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_public_document_decisions_account_document
  ON orcafacil.public_document_decisions (account_id, document_id, created_at DESC)
  WHERE is_deleted = false;

COMMIT;


-- >>> hotfix_saas_enterprise_modules_v66.sql
-- OrçaFácil V6.6 - SaaS modular enterprise (aditivo, idempotente e sem DROP).
CREATE SCHEMA IF NOT EXISTS orcafacil;

ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS alternate_email varchar(254);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS document_type varchar(16);
ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS document_number varchar(14);
ALTER TABLE orcafacil.users DROP CONSTRAINT IF EXISTS ck_users_role;
ALTER TABLE orcafacil.users ADD CONSTRAINT ck_users_role CHECK (role IN ('User','Admin','SuperAdmin','GlobalSupport','GlobalBilling','GlobalAuditor'));
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_alternate_email ON orcafacil.users(alternate_email) WHERE alternate_email IS NOT NULL AND is_deleted=false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_document_number ON orcafacil.users(document_number) WHERE document_number IS NOT NULL AND is_deleted=false;
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS city varchar(120);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS state varchar(2);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS responsible_name varchar(160);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS internal_notes varchar(2000);
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS financial_status varchar(32) NOT NULL DEFAULT 'Current';
ALTER TABLE orcafacil.business_accounts ADD COLUMN IF NOT EXISTS last_access_at timestamptz;

CREATE TABLE IF NOT EXISTS orcafacil.saas_modules (
 id uuid PRIMARY KEY, code varchar(80) NOT NULL, display_name varchar(160) NOT NULL, description varchar(1000) NOT NULL,
 category varchar(80) NOT NULL, icon_key varchar(80) NOT NULL, menu_group varchar(80) NOT NULL, route_prefix varchar(160) NOT NULL,
 required_permission_code varchar(160) NOT NULL, base_monthly_price numeric(18,2) NOT NULL, base_annual_price numeric(18,2) NOT NULL,
 is_active boolean NOT NULL DEFAULT true, is_public boolean NOT NULL DEFAULT true, display_order integer NOT NULL DEFAULT 0,
 deleted_at timestamptz, deleted_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_saas_modules_code ON orcafacil.saas_modules(code);
CREATE INDEX IF NOT EXISTS ix_saas_modules_status_order ON orcafacil.saas_modules(is_active,display_order);

CREATE TABLE IF NOT EXISTS orcafacil.saas_module_features (
 id uuid PRIMARY KEY, module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id), code varchar(120) NOT NULL, display_name varchar(160) NOT NULL,
 required_permission_code varchar(160) NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_saas_module_features_module_code ON orcafacil.saas_module_features(module_id,code);

CREATE TABLE IF NOT EXISTS orcafacil.saas_module_prices (
 id uuid PRIMARY KEY, module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id), billing_period varchar(16) NOT NULL, amount numeric(18,2) NOT NULL,
 currency varchar(3) NOT NULL DEFAULT 'BRL', valid_from timestamptz NOT NULL, valid_until timestamptz, is_active boolean NOT NULL DEFAULT true,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_saas_module_prices_module_period ON orcafacil.saas_module_prices(module_id,billing_period,valid_from);

CREATE TABLE IF NOT EXISTS orcafacil.account_module_subscriptions (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id),
 status varchar(32) NOT NULL, billing_period varchar(16) NOT NULL, contracted_price numeric(18,2) NOT NULL, discount_amount numeric(18,2) NOT NULL DEFAULT 0,
 currency varchar(3) NOT NULL DEFAULT 'BRL', starts_at timestamptz, trial_ends_at timestamptz, manual_grant_ends_at timestamptz,
 manual_grant_reason varchar(500), suspended_at timestamptz, cancelled_at timestamptz, changed_by_user_id uuid,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_subscriptions_active ON orcafacil.account_module_subscriptions(account_id,module_id) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_account_module_subscriptions_status ON orcafacil.account_module_subscriptions(account_id,status);

CREATE TABLE IF NOT EXISTS orcafacil.account_module_entitlements (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id),
 feature_code varchar(120), is_enabled boolean NOT NULL DEFAULT true, source varchar(24) NOT NULL, valid_until timestamptz, reason varchar(500), granted_by_user_id uuid NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_entitlements ON orcafacil.account_module_entitlements(account_id,module_id,COALESCE(feature_code,'')) WHERE is_deleted=false;

CREATE TABLE IF NOT EXISTS orcafacil.account_module_feature_limits (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_id uuid NOT NULL REFERENCES orcafacil.saas_modules(id),
 feature_code varchar(120) NOT NULL, limit_value bigint NOT NULL, period varchar(24) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_feature_limits ON orcafacil.account_module_feature_limits(account_id,module_id,feature_code,period) WHERE is_deleted=false;

CREATE TABLE IF NOT EXISTS orcafacil.account_module_usage_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), user_id uuid, module_code varchar(80) NOT NULL,
 feature_code varchar(120), event_code varchar(120) NOT NULL, route varchar(300), correlation_id varchar(100) NOT NULL, occurred_at timestamptz NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_account_module_usage_module ON orcafacil.account_module_usage_events(account_id,module_code,occurred_at);
CREATE INDEX IF NOT EXISTS ix_account_module_usage_user ON orcafacil.account_module_usage_events(account_id,user_id,occurred_at);

CREATE TABLE IF NOT EXISTS orcafacil.account_module_usage_snapshots (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), module_code varchar(80) NOT NULL,
 period_start timestamptz NOT NULL, period_end timestamptz NOT NULL, event_count bigint NOT NULL, active_users integer NOT NULL, last_used_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_module_usage_snapshots ON orcafacil.account_module_usage_snapshots(account_id,module_code,period_start) WHERE is_deleted=false;

CREATE TABLE IF NOT EXISTS orcafacil.account_module_audit_logs (
 id uuid PRIMARY KEY, account_id uuid, actor_user_id uuid NOT NULL, action varchar(120) NOT NULL, entity_type varchar(120) NOT NULL,
 entity_id varchar(100), module_code varchar(80), summary varchar(1000) NOT NULL, correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_account_module_audit_account ON orcafacil.account_module_audit_logs(account_id,created_at);
CREATE INDEX IF NOT EXISTS ix_account_module_audit_action ON orcafacil.account_module_audit_logs(action,created_at);

CREATE TABLE IF NOT EXISTS orcafacil.account_member_profiles (
 id uuid PRIMARY KEY, account_id uuid NOT NULL REFERENCES orcafacil.business_accounts(id), account_member_id uuid NOT NULL REFERENCES orcafacil.account_members(id),
 role_id uuid NOT NULL REFERENCES orcafacil.roles(id), assigned_by_user_id uuid NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_member_profiles ON orcafacil.account_member_profiles(account_id,account_member_id,role_id) WHERE is_deleted=false;

INSERT INTO orcafacil.roles(code,display_name,is_platform_role,is_system,created_at,is_deleted) VALUES
 ('Manager','Gestor',false,true,now(),false),('Commercial','Comercial',false,true,now(),false),
 ('Operations','Operações',false,true,now(),false),('Technician','Técnico',false,true,now(),false),
 ('Financial','Financeiro',false,true,now(),false),('Fiscal','Fiscal',false,true,now(),false),
 ('CustomerSuccess','Customer Success',false,true,now(),false),('Support','Suporte',false,true,now(),false),
 ('ReadOnly','Somente leitura',false,true,now(),false),('ClientPortalUser','Portal do cliente',false,true,now(),false),
 ('PartnerPortalUser','Portal do parceiro',false,true,now(),false)
ON CONFLICT(code) DO UPDATE SET display_name=EXCLUDED.display_name,is_deleted=false;

INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission,created_at,is_deleted) VALUES
 ('Global.Clients.View','Ver clientes globais',true,now(),false),('Global.Clients.Manage','Gerenciar clientes globais',true,now(),false),
 ('Global.Users.View','Ver usuários globais',true,now(),false),('Global.Users.Manage','Gerenciar usuários globais',true,now(),false),
 ('Global.Modules.View','Ver módulos globais',true,now(),false),('Global.Modules.Manage','Gerenciar módulos globais',true,now(),false),
 ('Global.Billing.View','Ver billing global',true,now(),false),('Global.Billing.Manage','Gerenciar billing global',true,now(),false),
 ('Global.Usage.View','Ver uso global',true,now(),false),('Global.Audit.View','Ver auditoria global',true,now(),false),
 ('Account.Users.View','Ver usuários da conta',false,now(),false),('Account.Users.Manage','Gerenciar usuários da conta',false,now(),false),
 ('Account.Profiles.Manage','Gerenciar perfis da conta',false,now(),false),('Account.Modules.View','Ver módulos da conta',false,now(),false),('Account.Billing.View','Ver billing da conta',false,now(),false),
 ('Clients.Create','Criar clientes',false,now(),false),('Clients.Edit','Editar clientes',false,now(),false),('Clients.Delete','Excluir clientes',false,now(),false),
 ('Documents.Delete','Excluir documentos',false,now(),false),('Documents.Approve','Aprovar documentos',false,now(),false),('Documents.Send','Enviar documentos',false,now(),false),('Documents.Convert','Converter documentos',false,now(),false),
 ('CommercialRoutine.View','Ver rotina comercial',false,now(),false),('CommercialRoutine.Manage','Gerenciar rotina comercial',false,now(),false),
 ('Projects.View','Ver projetos',false,now(),false),('Projects.Manage','Gerenciar projetos',false,now(),false),('Portal.View','Acessar portal',false,now(),false),
 ('Training.View','Ver treinamento',false,now(),false),('QualityGate.View','Ver quality gate',false,now(),false),('SystemHealth.View','Ver saúde do sistema',false,now(),false)
ON CONFLICT(code) DO UPDATE SET display_name=EXCLUDED.display_name,is_platform_permission=EXCLUDED.is_platform_permission,is_deleted=false;

INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p
WHERE r.code IN ('Owner','Administrator') AND p.is_platform_permission=false
ON CONFLICT(role_id,permission_id) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted)
SELECT r.id,p.id,now(),false FROM orcafacil.roles r JOIN orcafacil.permissions p ON
 (r.code='Manager' AND p.code NOT LIKE 'Global.%') OR
 (r.code='Commercial' AND (p.code LIKE 'Clients.%' OR p.code LIKE 'Documents.%' OR p.code LIKE 'CommercialRoutine.%' OR p.code='Dashboard.View')) OR
 (r.code IN ('Operations','Technician') AND (p.code LIKE 'WorkOrders.%' OR p.code LIKE 'Schedule.%' OR p.code='Dashboard.View')) OR
 (r.code='Financial' AND (p.code LIKE 'Finance.%' OR p.code LIKE 'Payments.%' OR p.code LIKE 'Receipts.%' OR p.code='Dashboard.View')) OR
 (r.code='Fiscal' AND (p.code LIKE 'Fiscal.%' OR p.code='Dashboard.View')) OR
 (r.code='CustomerSuccess' AND (p.code LIKE 'CustomerSuccess.%' OR p.code='Dashboard.View')) OR
 (r.code='Support' AND (p.code LIKE 'Support.%' OR p.code='Dashboard.View')) OR
 (r.code='ReadOnly' AND p.code IN ('Dashboard.View','Clients.View','Documents.View','WorkOrders.View','Finance.View','Fiscal.View')) OR
 (r.code='ClientPortalUser' AND p.code='Portal.View') OR (r.code='PartnerPortalUser' AND p.code='Partners.View')
ON CONFLICT(role_id,permission_id) DO NOTHING;

WITH module_seed(code,display_name,category,icon_key,menu_group,route_prefix,permission,monthly,annual,ordering,is_public,is_active) AS (VALUES
 ('CORE','Core','Base','home','Principal','/Dashboard','Dashboard.View',0,0,10,false,true),
 ('CLIENTS','Clientes','Comercial','users','Comercial','/Clients','Clients.View',39,390,20,true,true),
 ('DOCUMENTS','Orçamentos e Propostas','Comercial','file-text','Comercial','/Documents','Documents.View',69,690,30,true,true),
 ('COMMERCIAL_ROUTINE','Rotina Comercial','Comercial','trending-up','Comercial','/CommercialRoutine','CommercialRoutine.View',49,490,40,true,true),
 ('WORK_ORDERS','Ordens de Serviço','Operações','tool','Operações','/WorkOrders','WorkOrders.View',79,790,50,true,true),
 ('SCHEDULE','Agenda e Campo','Operações','calendar','Operações','/Schedule','Schedule.View',49,490,60,true,true),
 ('FINANCIAL','Financeiro','Gestão','dollar-sign','Gestão','/CashFlow','Finance.View',89,890,70,true,true),
 ('FISCAL','Fiscal','Gestão','clipboard','Gestão','/Fiscal','Fiscal.View',99,990,80,true,false),
 ('PROJECTS','Projetos','Gestão','briefcase','Gestão','/Projects','Projects.View',69,690,90,true,false),
 ('CUSTOMER_SUCCESS','Customer Success','Relacionamento','heart','Relacionamento','/CustomerSuccess','CustomerSuccess.View',59,590,100,true,false),
 ('BI','BI Executivo','Inteligência','bar-chart','Inteligência','/Bi','BI.View',99,990,110,true,false),
 ('AUTOMATION','Automação','Inteligência','zap','Inteligência','/Automation','Automation.View',79,790,120,true,false),
 ('DATA_GOVERNANCE','Governança de Dados','Governança','shield','Governança','/DataGovernance','DataQuality.View',89,890,130,true,false),
 ('CLIENT_PORTAL','Portal do Cliente','Portais','external-link','Portais','/Portal','Portal.View',49,490,140,true,false),
 ('PARTNER_PORTAL','Portal do Parceiro','Portais','link','Portais','/Partners','Partners.View',49,490,150,true,false),
 ('SUPPORT','Suporte','Sucesso','help-circle','Sucesso','/Support','Support.View',29,290,160,true,true),
 ('TRAINING','Treinamento','Sucesso','book-open','Sucesso','/Training','Training.View',19,190,170,true,true),
 ('ACCOUNT_ADMIN','Administração','Administração','settings','Administração','/AccountAdmin','Account.Users.View',0,0,180,false,true),
 ('QUALITY_GATE','Quality Gate','Governança','check-circle','Governança','/Admin/QualityGate','QualityGate.View',0,0,190,false,true))
INSERT INTO orcafacil.saas_modules(id,code,display_name,description,category,icon_key,menu_group,route_prefix,required_permission_code,base_monthly_price,base_annual_price,is_active,is_public,display_order,created_at,is_deleted)
SELECT md5('saas-module:'||code)::uuid,code,display_name,display_name,category,icon_key,menu_group,route_prefix,permission,monthly,annual,is_active,is_public,ordering,now(),false FROM module_seed
ON CONFLICT (code) DO UPDATE SET display_name=EXCLUDED.display_name, route_prefix=EXCLUDED.route_prefix, required_permission_code=EXCLUDED.required_permission_code, is_active=EXCLUDED.is_active, updated_at=now();

INSERT INTO orcafacil.saas_module_prices(id,module_id,billing_period,amount,currency,valid_from,is_active,created_at,is_deleted)
SELECT md5('saas-price-monthly:'||code)::uuid,id,'Monthly',base_monthly_price,'BRL',now(),true,now(),false FROM orcafacil.saas_modules
ON CONFLICT (id) DO NOTHING;
INSERT INTO orcafacil.saas_module_prices(id,module_id,billing_period,amount,currency,valid_from,is_active,created_at,is_deleted)
SELECT md5('saas-price-annual:'||code)::uuid,id,'Annual',base_annual_price,'BRL',now(),true,now(),false FROM orcafacil.saas_modules
ON CONFLICT (id) DO NOTHING;

-- Preserve existing customers: their previous all-in-one access becomes an explicit grandfathered contract.
INSERT INTO orcafacil.account_module_subscriptions(id,account_id,module_id,status,billing_period,contracted_price,discount_amount,currency,starts_at,created_at,is_deleted)
SELECT md5(a.id::text||':'||m.id::text)::uuid,a.id,m.id,'Active','Monthly',0,0,'BRL',now(),now(),false
FROM orcafacil.business_accounts a CROSS JOIN orcafacil.saas_modules m WHERE a.is_deleted=false
ON CONFLICT DO NOTHING;
INSERT INTO orcafacil.account_module_entitlements(id,account_id,module_id,feature_code,is_enabled,source,reason,granted_by_user_id,created_at,is_deleted)
SELECT md5('entitlement:'||a.id::text||':'||m.id::text)::uuid,a.id,m.id,NULL,true,'Subscription','Migração V6.6: acesso preexistente',COALESCE((SELECT am.user_id FROM orcafacil.account_members am WHERE am.account_id=a.id AND am.is_deleted=false ORDER BY am.created_at LIMIT 1),'00000000-0000-0000-0000-000000000000'::uuid),now(),false
FROM orcafacil.business_accounts a CROSS JOIN orcafacil.saas_modules m WHERE a.is_deleted=false
ON CONFLICT DO NOTHING;


-- >>> hotfix_schema_drift_quality_gate_v62.sql
-- V6.1 P0: reconcile the complete EF Document contract without dropping data.
-- Document.RowVersion is a C# byte[] application-managed concurrency token, hence bytea (not xmin/bigint).
BEGIN;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_code varchar(40);
UPDATE orcafacil.documents SET template_code = 'essential' WHERE template_code IS NULL OR btrim(template_code) = '';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET DEFAULT 'essential';
ALTER TABLE orcafacil.documents ALTER COLUMN template_code SET NOT NULL;

ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS template_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
UPDATE orcafacil.documents SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex') WHERE row_version IS NULL;
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');
ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_snapshot jsonb;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS conditions_text text;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS payment_method varchar(60);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS pix_information varchar(300);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS deposit_amount numeric(18,2);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS installment_count integer;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS estimated_duration varchar(120);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS expected_start_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS evidence_hash varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_status varchar(24) NOT NULL DEFAULT 'None';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS follow_up_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS current_wizard_step integer NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosave_key varchar(80);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS last_autosaved_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS public_token varchar(128);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision varchar(40) NOT NULL DEFAULT 'Pending';
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_at timestamptz;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_decision_note varchar(1000);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS internal_approval_status varchar(24);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS requires_internal_approval boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS converted_receipt_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS origin_budget_number varchar(40);
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_team_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS assigned_to_user_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS business_unit_id uuid;
ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS client_city varchar(120);

-- Refuse to conceal incompatible existing types; ADD COLUMN IF NOT EXISTS alone cannot repair those safely.
DO $validation$
DECLARE mismatch text;
BEGIN
  SELECT string_agg(expected.column_name || ' expected ' || expected.expected_type || ', found ' || coalesce(actual.data_type, '<missing>'), '; ')
    INTO mismatch
    FROM (VALUES
      ('template_code','character varying'), ('template_snapshot','jsonb'), ('row_version','bytea'),
      ('client_snapshot','jsonb'), ('conditions_text','text'), ('payment_method','character varying'),
      ('deposit_amount','numeric'), ('current_wizard_step','integer'), ('public_enabled','boolean')
    ) AS expected(column_name,expected_type)
    LEFT JOIN information_schema.columns actual
      ON actual.table_schema='orcafacil' AND actual.table_name='documents' AND actual.column_name=expected.column_name
   WHERE actual.data_type IS DISTINCT FROM expected.expected_type;
  IF mismatch IS NOT NULL THEN RAISE NOTICE 'documents schema differs from EF contract: %', mismatch; END IF;
END $validation$;

CREATE INDEX IF NOT EXISTS ix_documents_account_type_created ON orcafacil.documents(account_id, type, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_followup ON orcafacil.documents(account_id, type, next_follow_up_at);
CREATE INDEX IF NOT EXISTS ix_documents_account_type_valid_until ON orcafacil.documents(account_id, type, valid_until);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON orcafacil.documents(public_token) WHERE public_token IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_documents_template_code ON orcafacil.documents(account_id, template_code) WHERE template_code IS NOT NULL AND is_deleted = false;

COMMIT;

-- V6.2 consolidates the complete critical contract without destructive operations.
BEGIN;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS title varchar(160);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS profession varchar(120);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active ON orcafacil.budget_templates(account_id, is_active) WHERE is_deleted = false;
ALTER TABLE orcafacil.budget_template_items ADD COLUMN IF NOT EXISTS template_id uuid;
CREATE INDEX IF NOT EXISTS ix_budget_template_items_template ON orcafacil.budget_template_items(template_id);
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_system_template boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS deleted_by uuid;
COMMIT;

