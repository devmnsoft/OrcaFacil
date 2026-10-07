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
