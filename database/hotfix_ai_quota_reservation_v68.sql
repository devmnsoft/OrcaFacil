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
