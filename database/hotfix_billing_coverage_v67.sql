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
