-- =========================================================
-- OrçaFácil - Script completo PostgreSQL
-- Schema: orcafacil
-- =========================================================
-- Idempotência: este script usa CREATE IF NOT EXISTS, CREATE INDEX IF NOT EXISTS
-- e INSERT ... ON CONFLICT. Constraints definidas no CREATE TABLE só são aplicadas
-- automaticamente para tabelas novas; ajustes estruturais em tabelas legadas devem
-- ser adicionados em blocos DO com checagem explícita antes da implantação.

BEGIN;

-- 1. Extensões
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 2. Schema
CREATE SCHEMA IF NOT EXISTS orcafacil;

-- 3. Tabelas principais
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

-- 4. Tabelas de documentos
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

-- 5. Tabelas públicas
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

-- 6. Tabelas de cobrança
CREATE TABLE IF NOT EXISTS orcafacil.user_usage (id uuid DEFAULT gen_random_uuid(), user_id uuid NOT NULL, period varchar(7) NOT NULL, documents_created integer NOT NULL DEFAULT 0, budgets_created integer NOT NULL DEFAULT 0, receipts_created integer NOT NULL DEFAULT 0, pdf_generated integer NOT NULL DEFAULT 0, public_links_created integer NOT NULL DEFAULT 0, backup_exports integer NOT NULL DEFAULT 0, chatbot_questions integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_user_usage PRIMARY KEY (id), CONSTRAINT uq_orcafacil_user_usage_user_period UNIQUE (user_id, period), CONSTRAINT fk_orcafacil_user_usage_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS orcafacil.subscriptions (id uuid DEFAULT gen_random_uuid(), user_id uuid NOT NULL, provider varchar(40) NOT NULL DEFAULT 'Manual', status varchar(40) NOT NULL DEFAULT 'None', plan varchar(40) NOT NULL DEFAULT 'Free', billing_cycle varchar(40), amount numeric(18,2) NOT NULL DEFAULT 0, started_at timestamptz, expires_at timestamptz, cancelled_at timestamptz, last_payment_at timestamptz, external_customer_id varchar(180), external_subscription_id varchar(180), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_subscriptions PRIMARY KEY (id), CONSTRAINT fk_orcafacil_subscriptions_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS orcafacil.payments (id uuid DEFAULT gen_random_uuid(), user_id uuid NOT NULL, provider varchar(40) NOT NULL DEFAULT 'Manual', status varchar(40) NOT NULL DEFAULT 'Pending', plan varchar(40) NOT NULL DEFAULT 'Free', billing_cycle varchar(40), amount numeric(18,2) NOT NULL DEFAULT 0, currency varchar(10) NOT NULL DEFAULT 'BRL', external_payment_id varchar(180), external_preference_id varchar(180), payment_method varchar(80), payer_email varchar(254), approved_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_payments PRIMARY KEY (id), CONSTRAINT fk_orcafacil_payments_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE);

-- 7. Tabelas administrativas
CREATE TABLE IF NOT EXISTS orcafacil.admin_settings (id uuid DEFAULT gen_random_uuid(), key varchar(120) NOT NULL, value_json jsonb NOT NULL DEFAULT '{}'::jsonb, updated_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_admin_settings PRIMARY KEY (id), CONSTRAINT uq_orcafacil_admin_settings_key UNIQUE (key));
CREATE TABLE IF NOT EXISTS orcafacil.notifications (id uuid DEFAULT gen_random_uuid(), user_id uuid NOT NULL, title varchar(180) NOT NULL, message varchar(1000) NOT NULL, type varchar(60) NOT NULL DEFAULT 'Info', read boolean NOT NULL DEFAULT false, document_id uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_notifications PRIMARY KEY (id), CONSTRAINT fk_orcafacil_notifications_users FOREIGN KEY (user_id) REFERENCES orcafacil.users(id) ON DELETE CASCADE);

-- 8. Tabelas de logs/auditoria
CREATE TABLE IF NOT EXISTS orcafacil.audit_logs (id uuid DEFAULT gen_random_uuid(), user_id uuid, action varchar(120) NOT NULL, entity_type varchar(120) NOT NULL, entity_id varchar(120), before_json jsonb, after_json jsonb, metadata_json jsonb, ip_address varchar(80), user_agent varchar(1000), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_audit_logs PRIMARY KEY (id));
CREATE TABLE IF NOT EXISTS orcafacil.system_logs (id uuid DEFAULT gen_random_uuid(), level varchar(40) NOT NULL DEFAULT 'Info', type varchar(80) NOT NULL DEFAULT 'Application', message text NOT NULL, user_id uuid, user_email varchar(254), metadata_json jsonb, error_message text, error_stack text, environment varchar(80), url varchar(1000), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_system_logs PRIMARY KEY (id));
CREATE TABLE IF NOT EXISTS orcafacil.system_errors (id uuid DEFAULT gen_random_uuid(), message text NOT NULL, stack text, code varchar(80), severity varchar(40) NOT NULL DEFAULT 'Error', user_id uuid, context_json jsonb, resolved boolean NOT NULL DEFAULT false, resolved_at timestamptz, resolved_by uuid, admin_note varchar(1000), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, CONSTRAINT pk_orcafacil_system_errors PRIMARY KEY (id));

-- 9. Índices
CREATE INDEX IF NOT EXISTS ix_orcafacil_users_email ON orcafacil.users(email);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_user_id ON orcafacil.documents(user_id);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_created_at ON orcafacil.documents(created_at);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_status ON orcafacil.documents(status);
CREATE INDEX IF NOT EXISTS ix_orcafacil_documents_type ON orcafacil.documents(type);
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

-- 10. Seeds
INSERT INTO orcafacil.admin_settings (key, value_json, updated_at) VALUES
('company', '{"name":"OrçaFácil","country":"BR","footer":"MNSOFT"}', now()),
('contact', '{"email":"contato@example.com","phone":""}', now()),
('plans', '{"free":{"documents":20,"pdf":20,"watermark":true},"pro":{"documents":1000,"pdf":1000,"watermark":false}}', now()),
('logging', '{"level":"Information","retentionDays":30}', now()),
('security', '{"cookieAuth":true,"rateLimit":true}', now()),
('chatbot', '{"enabled":false}', now()),
('telegram', '{"enabled":false}', now()),
('theme', '{"primary":"#0d6efd"}', now()),
('terms', '{"version":"1.0","required":true}', now())
ON CONFLICT (key) DO UPDATE SET value_json = EXCLUDED.value_json, updated_at = now();

-- 11. Validação opcional
DO $$
DECLARE missing_count integer;
BEGIN
    SELECT count(*) INTO missing_count
      FROM unnest(ARRAY['users','issuer_profiles','documents','document_items','public_quotes','user_usage','subscriptions','payments','admin_settings','notifications','audit_logs','system_logs','system_errors']) AS required(table_name)
     WHERE to_regclass('orcafacil.' || required.table_name) IS NULL;

    IF missing_count > 0 THEN
        RAISE EXCEPTION 'Schema orcafacil incompleto: % tabelas obrigatórias ausentes.', missing_count;
    END IF;
END $$;

COMMIT;
