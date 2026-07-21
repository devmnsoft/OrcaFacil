-- OrçaFácil - script completo PostgreSQL 15+
-- Pode ser executado manualmente com psql e também pelo docker-entrypoint.
-- Idempotente para criação inicial: usa IF NOT EXISTS e seeds com ON CONFLICT.

BEGIN;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS billing;
CREATE SCHEMA IF NOT EXISTS admin;
CREATE SCHEMA IF NOT EXISTS logs;
CREATE SCHEMA IF NOT EXISTS public_access;

CREATE TABLE IF NOT EXISTS identity.users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(180) NOT NULL,
    email varchar(254) NOT NULL,
    phone varchar(40),
    password_hash text NOT NULL,
    role varchar(40) NOT NULL DEFAULT 'User',
    plan varchar(40) NOT NULL DEFAULT 'Free',
    is_active boolean NOT NULL DEFAULT true,
    is_blocked boolean NOT NULL DEFAULT false,
    block_reason varchar(500),
    accepted_terms_at timestamp with time zone,
    accepted_privacy_at timestamp with time zone,
    last_login_at timestamp with time zone,
    last_seen_at timestamp with time zone,
    is_deleted boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone,
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT ck_users_role CHECK (role IN ('User', 'Admin', 'SuperAdmin')),
    CONSTRAINT ck_users_plan CHECK (plan IN ('Free', 'Pro'))
);

CREATE TABLE IF NOT EXISTS core.issuer_profiles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id),
    business_name varchar(180) NOT NULL,
    document_number varchar(32),
    phone varchar(40),
    email varchar(254),
    address varchar(300),
    city varchar(120),
    pix_key varchar(180),
    logo_path varchar(500),
    is_deleted boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone,
    CONSTRAINT uq_issuer_profiles_user_id UNIQUE (user_id)
);

CREATE TABLE IF NOT EXISTS core.documents (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES identity.users(id),
    type varchar(40) NOT NULL,
    number varchar(40) NOT NULL,
    status varchar(40) NOT NULL,
    client_name varchar(180) NOT NULL,
    client_document varchar(32), client_phone varchar(40), client_email varchar(254), client_city varchar(120),
    issue_date timestamp with time zone NOT NULL DEFAULT now(), valid_until timestamp with time zone,
    notes varchar(4000), subtotal numeric(18,2) NOT NULL DEFAULT 0, discount numeric(18,2) NOT NULL DEFAULT 0, total numeric(18,2) NOT NULL DEFAULT 0,
    public_token varchar(128), public_enabled boolean NOT NULL DEFAULT false,
    client_decision varchar(40) NOT NULL DEFAULT 'Pending', client_decision_at timestamp with time zone, client_decision_note varchar(1000), evidence_hash varchar(128),
    origin_budget_id uuid, origin_budget_number varchar(40), converted_receipt_id uuid, converted_receipt_number varchar(40),
    is_deleted boolean NOT NULL DEFAULT false, deleted_at timestamp with time zone, deleted_by uuid,
    created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone,
    CONSTRAINT uq_documents_user_type_number UNIQUE (user_id, type, number),
    CONSTRAINT ck_documents_type CHECK (type IN ('Budget', 'Receipt')),
    CONSTRAINT ck_documents_client_decision CHECK (client_decision IN ('Pending', 'Approved', 'Rejected'))
);

CREATE TABLE IF NOT EXISTS core.document_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), document_id uuid NOT NULL REFERENCES core.documents(id) ON DELETE CASCADE,
    description varchar(500) NOT NULL, quantity numeric(18,4) NOT NULL DEFAULT 1, unit_price numeric(18,2) NOT NULL DEFAULT 0, discount numeric(18,2) NOT NULL DEFAULT 0, total numeric(18,2) NOT NULL DEFAULT 0,
    is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE TABLE IF NOT EXISTS public_access.public_quotes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), token varchar(128) NOT NULL UNIQUE,
    owner_user_id uuid NOT NULL REFERENCES identity.users(id), document_id uuid NOT NULL REFERENCES core.documents(id),
    public_enabled boolean NOT NULL DEFAULT true, expires_at timestamp with time zone, view_count integer NOT NULL DEFAULT 0, last_access_at timestamp with time zone,
    decision_status varchar(40) NOT NULL DEFAULT 'Pending', decision_note varchar(1000), decided_at timestamp with time zone,
    decided_by_name varchar(180), decided_by_document varchar(32), decided_by_email varchar(254), accepted_terms boolean NOT NULL DEFAULT false,
    evidence_hash varchar(128), user_agent varchar(1000), is_deleted boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone,
    CONSTRAINT ck_public_quotes_decision_status CHECK (decision_status IN ('Pending', 'Approved', 'Rejected'))
);

CREATE TABLE IF NOT EXISTS core.user_usage (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES identity.users(id), period varchar(6) NOT NULL,
    documents_created integer NOT NULL DEFAULT 0, budgets_created integer NOT NULL DEFAULT 0, receipts_created integer NOT NULL DEFAULT 0, pdf_generated integer NOT NULL DEFAULT 0,
    public_links_created integer NOT NULL DEFAULT 0, backup_exports integer NOT NULL DEFAULT 0, chatbot_questions integer NOT NULL DEFAULT 0,
    is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone,
    CONSTRAINT uq_user_usage_user_period UNIQUE (user_id, period)
);

CREATE TABLE IF NOT EXISTS billing.subscriptions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES identity.users(id), provider varchar(40) NOT NULL DEFAULT 'Manual', status varchar(40) NOT NULL DEFAULT 'None', plan varchar(40) NOT NULL DEFAULT 'Free', billing_cycle varchar(40), amount numeric(18,2) NOT NULL DEFAULT 0,
    started_at timestamp with time zone, expires_at timestamp with time zone, cancelled_at timestamp with time zone, last_payment_at timestamp with time zone,
    external_customer_id varchar(200), external_subscription_id varchar(200), is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE TABLE IF NOT EXISTS billing.payments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES identity.users(id), provider varchar(40) NOT NULL DEFAULT 'Manual', status varchar(40) NOT NULL DEFAULT 'Pending', plan varchar(40) NOT NULL DEFAULT 'Free', billing_cycle varchar(40), amount numeric(18,2) NOT NULL DEFAULT 0, currency varchar(10) NOT NULL DEFAULT 'BRL',
    external_payment_id varchar(200), external_preference_id varchar(200), payment_method varchar(100), payer_email varchar(254), approved_at timestamp with time zone, is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE TABLE IF NOT EXISTS admin.admin_settings (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), key varchar(120) NOT NULL UNIQUE, value_json jsonb NOT NULL DEFAULT '{}'::jsonb, updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE TABLE IF NOT EXISTS admin.notifications (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES identity.users(id), title varchar(180) NOT NULL, message varchar(1000) NOT NULL, type varchar(60) NOT NULL DEFAULT 'Info', read boolean NOT NULL DEFAULT false, document_id uuid,
    is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE TABLE IF NOT EXISTS logs.audit_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid, action varchar(120) NOT NULL, entity_type varchar(120) NOT NULL, entity_id varchar(120), before_json jsonb, after_json jsonb, metadata_json jsonb, ip_address varchar(80), user_agent varchar(1000),
    is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE TABLE IF NOT EXISTS logs.system_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), level varchar(40) NOT NULL, type varchar(120) NOT NULL, message varchar(2000) NOT NULL, user_id uuid, user_email varchar(254), metadata_json jsonb, error_message varchar(2000), error_stack text, environment varchar(120), url varchar(1000),
    is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE TABLE IF NOT EXISTS logs.system_errors (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), message varchar(2000) NOT NULL, stack text, code varchar(120), severity varchar(40) NOT NULL DEFAULT 'Error', user_id uuid, context_json jsonb, resolved boolean NOT NULL DEFAULT false, resolved_at timestamp with time zone, resolved_by uuid, admin_note varchar(1000),
    is_deleted boolean NOT NULL DEFAULT false, created_at timestamp with time zone NOT NULL DEFAULT now(), updated_at timestamp with time zone
);

CREATE INDEX IF NOT EXISTS ix_users_email ON identity.users(email);
CREATE INDEX IF NOT EXISTS ix_documents_user_id ON core.documents(user_id);
CREATE INDEX IF NOT EXISTS ix_documents_created_at ON core.documents(created_at);
CREATE INDEX IF NOT EXISTS ix_documents_status ON core.documents(status);
CREATE INDEX IF NOT EXISTS ix_documents_type ON core.documents(type);
CREATE INDEX IF NOT EXISTS ix_documents_public_token ON core.documents(public_token);
CREATE INDEX IF NOT EXISTS ix_document_items_document_id ON core.document_items(document_id);
CREATE INDEX IF NOT EXISTS ix_public_quotes_token ON public_access.public_quotes(token);
CREATE INDEX IF NOT EXISTS ix_public_quotes_document_id ON public_access.public_quotes(document_id);
CREATE INDEX IF NOT EXISTS ix_user_usage_user_period ON core.user_usage(user_id, period);
CREATE INDEX IF NOT EXISTS ix_audit_logs_created_at ON logs.audit_logs(created_at);
CREATE INDEX IF NOT EXISTS ix_system_logs_created_at ON logs.system_logs(created_at);
CREATE INDEX IF NOT EXISTS ix_system_errors_created_at ON logs.system_errors(created_at);
CREATE INDEX IF NOT EXISTS ix_system_errors_resolved ON logs.system_errors(resolved);
CREATE INDEX IF NOT EXISTS ix_notifications_user_id ON admin.notifications(user_id);

INSERT INTO admin.admin_settings (key, value_json, updated_at) VALUES
('company', '{"companyName":"MNSOFT","companyCnpj":"18.160.057/0001-13","companyEmail":"comercial@mnsoft.com.br","supportEmail":"comercial@mnsoft.com.br"}'::jsonb, now()),
('contact', '{"whatsappEnabled":true,"whatsappNumber":"5591981809035","whatsappDisplayName":"Atendimento MNSOFT","whatsappDefaultMessage":"Olá, gostaria de falar com a MNSOFT sobre o OrçaFácil.","emailEnabled":true,"email":"comercial@mnsoft.com.br"}'::jsonb, now()),
('plans', '{"free":{"name":"Free","priceMonthly":0,"maxDocumentsPerMonth":20,"maxPdfPerMonth":20,"watermark":true},"pro":{"name":"Pro","priceMonthly":19.90,"priceYearly":199.00,"maxDocumentsPerMonth":null,"maxPdfPerMonth":null,"watermark":false}}'::jsonb, now()),
('logging', '{"level":"Information","retentionDays":90,"auditEnabled":true}'::jsonb, now()),
('security', '{"requireHttps":true,"cookieAuthentication":true,"passwordMinLength":8}'::jsonb, now()),
('chatbot', '{"enabled":false,"provider":"None"}'::jsonb, now()),
('telegram', '{"enabled":false,"botToken":"","chatId":""}'::jsonb, now()),
('theme', '{"name":"default","primaryColor":"#0d6efd","darkMode":false}'::jsonb, now()),
('terms', '{"version":"1.0","termsUrl":"/terms","privacyUrl":"/privacy"}'::jsonb, now())
ON CONFLICT (key) DO UPDATE SET value_json = EXCLUDED.value_json, updated_at = now();

-- SuperAdmin: não há senha fixa neste script. Use ORCAFACIL_ADMIN_EMAIL e ORCAFACIL_ADMIN_PASSWORD na aplicação.

COMMIT;
