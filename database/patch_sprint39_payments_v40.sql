-- Sprint 39 / V4.0. Additive and idempotent: never drops or truncates customer data.
CREATE SCHEMA IF NOT EXISTS orcafacil;

CREATE TABLE IF NOT EXISTS orcafacil.payment_providers (
 id uuid PRIMARY KEY, provider_name varchar(80) NOT NULL, environment varchar(20) NOT NULL,
 status varchar(24) NOT NULL DEFAULT 'NotConfigured', is_active boolean NOT NULL DEFAULT false,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_payment_provider_environment CHECK (environment IN ('Sandbox','Production','Manual'))
);
CREATE TABLE IF NOT EXISTS orcafacil.payment_provider_accounts (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, provider_id uuid NOT NULL REFERENCES orcafacil.payment_providers(id),
 public_key_masked varchar(160), protected_secret_key text, protected_webhook_secret text,
 last_health_check_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(account_id, provider_id)
);
CREATE TABLE IF NOT EXISTS orcafacil.payment_provider_health_checks (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, provider_id uuid NOT NULL, status varchar(24) NOT NULL,
 sanitized_error text, checked_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.payment_customers (id uuid PRIMARY KEY, account_id uuid NOT NULL, customer_id uuid, provider_id uuid, external_customer_id varchar(180), created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,provider_id,external_customer_id));
CREATE TABLE IF NOT EXISTS orcafacil.payment_methods (id uuid PRIMARY KEY, account_id uuid NOT NULL, payment_customer_id uuid, method_type varchar(24) NOT NULL, provider_token_reference varchar(180), brand varchar(30), last_four char(4), is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), CONSTRAINT ck_payment_method_last_four CHECK(last_four IS NULL OR last_four ~ '^[0-9]{4}$'));
CREATE TABLE IF NOT EXISTS orcafacil.payment_checkout_sessions (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, customer_id uuid, invoice_id uuid, purpose varchar(40) NOT NULL,
 amount numeric(18,2) NOT NULL CHECK(amount > 0), currency char(3) NOT NULL DEFAULT 'BRL', status varchar(24) NOT NULL DEFAULT 'Pending',
 provider_session_id varchar(180), idempotency_key varchar(160) NOT NULL, expires_at timestamptz NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,idempotency_key)
);
CREATE TABLE IF NOT EXISTS orcafacil.payment_checkout_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, checkout_session_id uuid NOT NULL REFERENCES orcafacil.payment_checkout_sessions(id), event_type varchar(80) NOT NULL, sanitized_payload jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_invoices (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, customer_id uuid, subscription_id uuid, period_start date, period_end date,
 subtotal numeric(18,2) NOT NULL, discount_total numeric(18,2) NOT NULL DEFAULT 0, tax_total numeric(18,2) NOT NULL DEFAULT 0,
 total numeric(18,2) NOT NULL CHECK(total >= 0), paid_total numeric(18,2) NOT NULL DEFAULT 0, due_at timestamptz NOT NULL,
 status varchar(24) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.payment_invoice_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, invoice_id uuid NOT NULL REFERENCES orcafacil.payment_invoices(id), description varchar(300) NOT NULL, quantity numeric(18,4) NOT NULL CHECK(quantity > 0), unit_amount numeric(18,2) NOT NULL CHECK(unit_amount >= 0), total numeric(18,2) NOT NULL);
CREATE TABLE IF NOT EXISTS orcafacil.payment_invoice_discounts (id uuid PRIMARY KEY, account_id uuid NOT NULL, invoice_id uuid NOT NULL REFERENCES orcafacil.payment_invoices(id), coupon_code varchar(80), description varchar(200) NOT NULL, amount numeric(18,2) NOT NULL CHECK(amount >= 0));
CREATE TABLE IF NOT EXISTS orcafacil.payment_invoice_taxes (id uuid PRIMARY KEY, account_id uuid NOT NULL, invoice_id uuid NOT NULL REFERENCES orcafacil.payment_invoices(id), description varchar(200) NOT NULL, amount numeric(18,2) NOT NULL CHECK(amount >= 0));
CREATE TABLE IF NOT EXISTS orcafacil.payment_transactions (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, invoice_id uuid, provider_id uuid, external_id varchar(180), method varchar(24) NOT NULL,
 origin varchar(16) NOT NULL, amount numeric(18,2) NOT NULL CHECK(amount > 0), status varchar(24) NOT NULL,
 confirmed_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(provider_id,external_id)
);
CREATE TABLE IF NOT EXISTS orcafacil.payment_charges (id uuid PRIMARY KEY, account_id uuid NOT NULL, invoice_id uuid, amount numeric(18,2) NOT NULL CHECK(amount > 0), currency char(3) NOT NULL DEFAULT 'BRL', status varchar(24) NOT NULL, idempotency_key varchar(160) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,idempotency_key));
CREATE TABLE IF NOT EXISTS orcafacil.payment_charge_attempts (id uuid PRIMARY KEY, account_id uuid NOT NULL, charge_id uuid NOT NULL REFERENCES orcafacil.payment_charges(id), provider_id uuid, attempt_number integer NOT NULL CHECK(attempt_number > 0), status varchar(24) NOT NULL, sanitized_error text, attempted_at timestamptz NOT NULL DEFAULT now(), UNIQUE(charge_id,attempt_number));
CREATE TABLE IF NOT EXISTS orcafacil.payment_transaction_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, transaction_id uuid NOT NULL REFERENCES orcafacil.payment_transactions(id), event_type varchar(80) NOT NULL, sanitized_payload jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_pix_codes (id uuid PRIMARY KEY, account_id uuid NOT NULL, transaction_id uuid NOT NULL, status varchar(24) NOT NULL, copy_paste_payload text, qr_code_provider_url text, expires_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_bank_slips (id uuid PRIMARY KEY, account_id uuid NOT NULL, transaction_id uuid NOT NULL, status varchar(24) NOT NULL, barcode varchar(160), provider_url text, due_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_card_authorizations (id uuid PRIMARY KEY, account_id uuid NOT NULL, transaction_id uuid NOT NULL, brand varchar(30) NOT NULL, last_four char(4) NOT NULL, provider_token_reference varchar(180), provider_authorization_id varchar(180), status varchar(24) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), CONSTRAINT ck_card_last_four CHECK(last_four ~ '^[0-9]{4}$'));
CREATE TABLE IF NOT EXISTS orcafacil.payment_webhook_endpoints (id uuid PRIMARY KEY, account_id uuid NOT NULL, provider_id uuid NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_webhook_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, provider varchar(80) NOT NULL, provider_event_id varchar(180) NOT NULL,
 event_type varchar(80) NOT NULL, signature_valid boolean NOT NULL, status varchar(24) NOT NULL,
 sanitized_payload jsonb NOT NULL DEFAULT '{}'::jsonb, received_at timestamptz NOT NULL DEFAULT now(), processed_at timestamptz,
 UNIQUE(account_id,provider,provider_event_id)
);
CREATE TABLE IF NOT EXISTS orcafacil.payment_webhook_processing_logs (id uuid PRIMARY KEY, account_id uuid NOT NULL, webhook_event_id uuid NOT NULL, outcome varchar(40) NOT NULL, sanitized_error text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_reconciliation_batches (id uuid PRIMARY KEY, account_id uuid NOT NULL, source varchar(24) NOT NULL, status varchar(24) NOT NULL, imported_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz);
CREATE TABLE IF NOT EXISTS orcafacil.payment_reconciliation_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, batch_id uuid NOT NULL REFERENCES orcafacil.payment_reconciliation_batches(id), transaction_id uuid, external_reference varchar(180) NOT NULL, expected_amount numeric(18,2), settled_amount numeric(18,2) NOT NULL, difference numeric(18,2) NOT NULL DEFAULT 0, status varchar(24) NOT NULL, approved_by_user_id uuid, approval_reason text);
CREATE TABLE IF NOT EXISTS orcafacil.payment_refunds (id uuid PRIMARY KEY, account_id uuid NOT NULL, transaction_id uuid NOT NULL, amount numeric(18,2) NOT NULL CHECK(amount > 0), status varchar(24) NOT NULL, reason text NOT NULL, requested_by_user_id uuid NOT NULL, provider_refund_id varchar(180), created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz);
CREATE TABLE IF NOT EXISTS orcafacil.payment_disputes (id uuid PRIMARY KEY, account_id uuid NOT NULL, transaction_id uuid NOT NULL, provider_dispute_id varchar(180), status varchar(24) NOT NULL, response_due_at timestamptz, reason text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_receipts (id uuid PRIMARY KEY, account_id uuid NOT NULL, transaction_id uuid NOT NULL, receipt_id uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,transaction_id));
CREATE TABLE IF NOT EXISTS orcafacil.payment_subscription_charges (id uuid PRIMARY KEY, account_id uuid NOT NULL, subscription_id uuid NOT NULL, invoice_id uuid NOT NULL, period_start date NOT NULL, period_end date NOT NULL, status varchar(24) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,subscription_id,period_start,period_end));
CREATE TABLE IF NOT EXISTS orcafacil.saas_subscription_invoices (id uuid PRIMARY KEY, account_id uuid NOT NULL, subscription_id uuid NOT NULL, payment_invoice_id uuid NOT NULL, period_start date NOT NULL, period_end date NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,subscription_id,period_start,period_end));
CREATE TABLE IF NOT EXISTS orcafacil.saas_subscription_payment_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, subscription_id uuid NOT NULL, transaction_id uuid, event_type varchar(80) NOT NULL, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.saas_billing_dunning_rules (id uuid PRIMARY KEY, account_id uuid, days_offset integer NOT NULL, action varchar(40) NOT NULL, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,days_offset,action));
CREATE TABLE IF NOT EXISTS orcafacil.saas_billing_dunning_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, invoice_id uuid NOT NULL, rule_id uuid, status varchar(24) NOT NULL, channel varchar(24), occurred_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id,invoice_id,rule_id));
CREATE TABLE IF NOT EXISTS orcafacil.saas_account_suspension_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, previous_status varchar(24) NOT NULL, new_status varchar(24) NOT NULL, reason text NOT NULL, actor_user_id uuid, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS orcafacil.payment_audit_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, actor_user_id uuid, event_type varchar(100) NOT NULL, entity_type varchar(80) NOT NULL, entity_id varchar(180) NOT NULL, sanitized_detail jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NOT NULL DEFAULT now());

CREATE INDEX IF NOT EXISTS ix_payment_invoice_account_status_due ON orcafacil.payment_invoices(account_id,status,due_at);
CREATE INDEX IF NOT EXISTS ix_payment_transaction_account_status ON orcafacil.payment_transactions(account_id,status,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_payment_webhook_pending ON orcafacil.payment_webhook_events(status,received_at) WHERE processed_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_payment_reconciliation_account_status ON orcafacil.payment_reconciliation_items(account_id,status);
CREATE INDEX IF NOT EXISTS ix_payment_audit_account_time ON orcafacil.payment_audit_events(account_id,occurred_at DESC);

COMMENT ON TABLE orcafacil.payment_card_authorizations IS 'Stores only brand, last four digits and opaque provider references. PAN and CVV are forbidden.';
