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
    CONSTRAINT ck_orcafacil_users_plan CHECK (plan IN ('Free', 'Pro'))
);

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
    person_type varchar(30) NOT NULL DEFAULT 'Individual', document_type varchar(10), document_number varchar(20), name varchar(180) NOT NULL, trade_name varchar(180), legal_name varchar(180), email varchar(254), phone varchar(40), city varchar(120), address varchar(300), mercadopago_customer_id varchar(180),
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
        RAISE EXCEPTION 'Schema orcafacil incompleto: % tabelas obrigatórias ausentes.', missing_count;
    END IF;
END $$;

COMMIT;
