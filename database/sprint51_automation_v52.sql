-- Sprint 51 / V5.2 - núcleo transacional do motor de automação.
-- Idempotente, aditivo e isolado por conta. Não remove nem reescreve dados existentes.
BEGIN;
CREATE SCHEMA IF NOT EXISTS orcafacil;

CREATE TABLE IF NOT EXISTS orcafacil.automation_rules (
    id uuid PRIMARY KEY, account_id uuid NULL, name varchar(160) NOT NULL,
    description varchar(1000), status varchar(32) NOT NULL DEFAULT 'Draft',
    owner_id uuid NOT NULL, current_version integer NOT NULL DEFAULT 0,
    is_global boolean NOT NULL DEFAULT false, paused_reason varchar(500),
    created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_automation_rule_scope CHECK ((is_global AND account_id IS NULL) OR (NOT is_global AND account_id IS NOT NULL))
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_versions (
    id uuid PRIMARY KEY, rule_id uuid NOT NULL REFERENCES orcafacil.automation_rules(id), version integer NOT NULL,
    definition_json jsonb NOT NULL, created_by uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(rule_id, version)
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_execution_runs (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, rule_id uuid NOT NULL REFERENCES orcafacil.automation_rules(id),
    version integer NOT NULL, trigger_code varchar(100) NOT NULL, status varchar(32) NOT NULL,
    idempotency_key varchar(240) NOT NULL, correlation_id varchar(100) NOT NULL,
    payload_summary jsonb NOT NULL DEFAULT '{}'::jsonb, started_at timestamptz, completed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id, idempotency_key)
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_execution_steps (
    id uuid PRIMARY KEY, run_id uuid NOT NULL REFERENCES orcafacil.automation_rule_execution_runs(id),
    sequence integer NOT NULL, step_type varchar(32) NOT NULL, code varchar(100) NOT NULL,
    status varchar(32) NOT NULL, sanitized_result jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(run_id, sequence)
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_event_queue (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, trigger_code varchar(100) NOT NULL,
    idempotency_key varchar(240) NOT NULL, payload_json jsonb NOT NULL, status varchar(32) NOT NULL DEFAULT 'Queued',
    attempt_count integer NOT NULL DEFAULT 0, available_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(account_id, idempotency_key)
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_event_dead_letters (
    id uuid PRIMARY KEY, queue_event_id uuid NOT NULL, account_id uuid NOT NULL,
    sanitized_error varchar(2000) NOT NULL, attempts integer NOT NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_dry_runs (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, rule_id uuid NULL, input_json jsonb NOT NULL,
    output_json jsonb NOT NULL, created_by uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_approval_requests (
    id uuid PRIMARY KEY, account_id uuid NOT NULL, rule_id uuid NOT NULL, action_code varchar(100) NOT NULL,
    requested_by uuid NOT NULL, decided_by uuid, status varchar(32) NOT NULL DEFAULT 'Pending', reason varchar(1000),
    requested_at timestamptz NOT NULL DEFAULT now(), decided_at timestamptz
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_safety_policies (
    id uuid PRIMARY KEY, account_id uuid NULL, max_executions_per_hour integer NOT NULL DEFAULT 100,
    max_consecutive_failures integer NOT NULL DEFAULT 5, require_critical_approval boolean NOT NULL DEFAULT true,
    block_outside_business_hours boolean NOT NULL DEFAULT false, updated_by uuid NOT NULL, updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(account_id)
);
CREATE TABLE IF NOT EXISTS orcafacil.automation_rule_audit_events (
    id uuid PRIMARY KEY, account_id uuid NULL, rule_id uuid NULL, event_type varchar(100) NOT NULL,
    actor_id uuid, correlation_id varchar(100) NOT NULL, sanitized_details jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_automation_rules_account_status ON orcafacil.automation_rules(account_id,status);
CREATE INDEX IF NOT EXISTS ix_automation_runs_account_created ON orcafacil.automation_rule_execution_runs(account_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_automation_queue_available ON orcafacil.automation_rule_event_queue(status,available_at);
CREATE INDEX IF NOT EXISTS ix_automation_approvals_account_status ON orcafacil.automation_rule_approval_requests(account_id,status);
COMMIT;
