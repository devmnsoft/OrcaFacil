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
        RAISE EXCEPTION 'Schema orcafacil incompleto: % tabelas obrigatórias ausentes.', missing_count;
    END IF;
END $$;


-- Modelos de orçamento por profissão (idempotente)
CREATE TABLE IF NOT EXISTS orcafacil.budget_templates (
    id uuid PRIMARY KEY,
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
INSERT INTO orcafacil.plans (code,display_name,short_description,is_free,is_recommended,display_order) VALUES ('FREE','Grátis','Para começar a organizar seus documentos.',true,false,1),('PROFESSIONAL','Profissional','Para apresentar seu trabalho com mais profissionalismo.',false,true,2),('BUSINESS','Negócio','Para acompanhar clientes e trabalhar em equipe.',false,false,3) ON CONFLICT(code) DO UPDATE SET display_name=excluded.display_name,short_description=excluded.short_description;
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
SELECT md5(p.code || ':v1')::uuid,p.id,1,v.monthly,v.annual,'BRL',now(),'Published',now() FROM orcafacil.plans p JOIN (VALUES ('FREE',0::numeric,0::numeric),('PROFESSIONAL',24.90,249),('BUSINESS',49.90,499)) v(code,monthly,annual) ON v.code=p.code ON CONFLICT(plan_id,version_number) DO NOTHING;
INSERT INTO orcafacil.features(id,code,display_name,description,value_type,category)
SELECT md5(code)::uuid,code,name,name,type,category FROM (VALUES
('team.members_limit','Pessoas da equipe','Integer','Equipe'),('clients.active_limit','Clientes ativos','Integer','Clientes'),('services.active_limit','Serviços ativos','Integer','Serviços'),('documents.monthly_limit','Documentos mensais','Integer','Documentos'),('pdf.monthly_limit','PDFs mensais','Integer','Documentos'),('pdf.watermark','Marca OrçaFácil','Boolean','Documentos'),('branding.custom_logo','Logo próprio','Boolean','Marca'),('history.days_visible','Histórico visível','Integer','Histórico'),('templates.basic_limit','Modelos básicos','Integer','Modelos'),('templates.custom_enabled','Modelos personalizados','Boolean','Modelos'),('public_approval.enabled','Aprovação pública','Boolean','Documentos'),('public_approval.monthly_limit','Aprovações mensais','Integer','Documentos'),('document.convert_to_receipt','Conversão em recibo','Boolean','Documentos'),('sharing.whatsapp','Compartilhamento por WhatsApp','Boolean','Compartilhamento'),('sharing.public_link','Link público','Boolean','Compartilhamento'),('numbering.custom_prefix','Prefixo personalizado','Boolean','Documentos'),('commercial.pipeline','Pipeline comercial','Boolean','Comercial'),('commercial.followups','Acompanhamentos','Boolean','Comercial'),('commercial.metrics','Indicadores comerciais','Boolean','Comercial'),('reports.basic','Relatórios básicos','Boolean','Relatórios'),('reports.advanced','Relatórios avançados','Boolean','Relatórios'),('exports.csv','Exportação CSV','Boolean','Relatórios'),('audit.account','Auditoria da conta','Boolean','Auditoria'),('support.priority','Suporte prioritário','Boolean','Suporte')) f(code,name,type,category) ON CONFLICT(code) DO NOTHING;

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
