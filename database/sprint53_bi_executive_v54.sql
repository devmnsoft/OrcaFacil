-- Sprint 53 / V5.4. Additive and idempotent executive BI schema.
CREATE SCHEMA IF NOT EXISTS orcafacil;

CREATE TABLE IF NOT EXISTS orcafacil.bi_metric_definitions (
    id uuid PRIMARY KEY, account_id uuid NULL, code varchar(100) NOT NULL,
    name varchar(180) NOT NULL, category varchar(80) NOT NULL, formula text NOT NULL,
    source varchar(160) NOT NULL, is_sensitive boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true, version integer NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_bi_metric_account_code UNIQUE NULLS NOT DISTINCT (account_id, code)
);
CREATE TABLE IF NOT EXISTS orcafacil.bi_metric_snapshots (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, metric_id uuid NOT NULL REFERENCES orcafacil.bi_metric_definitions(id),
    period_start date NOT NULL, period_end date NOT NULL, value numeric(20,4), source varchar(160) NOT NULL,
    calculated_at timestamptz NOT NULL, duration_ms bigint NOT NULL,
    CONSTRAINT ck_bi_snapshot_period CHECK (period_end >= period_start),
    CONSTRAINT uq_bi_snapshot UNIQUE (account_id, metric_id, period_start, period_end)
);
CREATE TABLE IF NOT EXISTS orcafacil.bi_dashboards (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(180) NOT NULL,
    period_start date NOT NULL, period_end date NOT NULL, is_global boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.bi_goals (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, metric_id uuid NOT NULL REFERENCES orcafacil.bi_metric_definitions(id),
    name varchar(180) NOT NULL, target numeric(20,4) NOT NULL, period_start date NOT NULL,
    period_end date NOT NULL, status varchar(30) NOT NULL, responsible_user_id uuid NULL,
    cancellation_reason text NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.bi_okr_cycles (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(180) NOT NULL,
    period_start date NOT NULL, period_end date NOT NULL, status varchar(30) NOT NULL,
    CONSTRAINT ck_bi_okr_cycle_period CHECK (period_end >= period_start)
);
CREATE TABLE IF NOT EXISTS orcafacil.bi_objectives (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, cycle_id uuid NOT NULL REFERENCES orcafacil.bi_okr_cycles(id),
    name varchar(220) NOT NULL, status varchar(30) NOT NULL, responsible_user_id uuid NULL
);
CREATE TABLE IF NOT EXISTS orcafacil.bi_key_results (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, objective_id uuid NOT NULL REFERENCES orcafacil.bi_objectives(id),
    name varchar(220) NOT NULL, metric_id uuid NULL REFERENCES orcafacil.bi_metric_definitions(id),
    target numeric(20,4) NOT NULL, current_value numeric(20,4) NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS orcafacil.bi_alert_events (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, rule_code varchar(100) NOT NULL,
    severity varchar(20) NOT NULL, reason text NOT NULL, source_url varchar(400) NOT NULL,
    period_start date NOT NULL, period_end date NOT NULL, acknowledged_at timestamptz NULL,
    resolved_at timestamptz NULL, resolution text NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS uq_bi_alert_open ON orcafacil.bi_alert_events(account_id, rule_code, period_start, period_end) WHERE resolved_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_bi_snapshots_account_period ON orcafacil.bi_metric_snapshots(account_id, period_start, period_end);
CREATE INDEX IF NOT EXISTS ix_bi_goals_account_period ON orcafacil.bi_goals(account_id, period_start, period_end);
CREATE INDEX IF NOT EXISTS ix_bi_objectives_account_cycle ON orcafacil.bi_objectives(account_id, cycle_id);

