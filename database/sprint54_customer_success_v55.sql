-- Sprint 54 / V5.5 - Customer Success. Additive and safe for existing tenants.
CREATE SCHEMA IF NOT EXISTS orcafacil;

CREATE TABLE IF NOT EXISTS orcafacil.customer_success_accounts (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL,
  owner_id uuid, onboarding_status varchar(40) NOT NULL DEFAULT 'NotStarted',
  created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cs_accounts_scope ON orcafacil.customer_success_accounts(account_id, client_id);

CREATE TABLE IF NOT EXISTS orcafacil.customer_success_health_rules (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, code varchar(80) NOT NULL,
  weight numeric(7,2) NOT NULL CHECK(weight > 0 AND weight <= 100), threshold numeric(18,4) NOT NULL,
  higher_is_better boolean NOT NULL, active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cs_health_rule ON orcafacil.customer_success_health_rules(account_id, code);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_health_scores (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, score numeric(5,2),
  band varchar(30) NOT NULL, calculated_at timestamptz NOT NULL, source_fingerprint varchar(128) NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_cs_health_scope_date ON orcafacil.customer_success_health_scores(account_id, client_id, calculated_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_health_score_factors (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, health_score_id uuid NOT NULL REFERENCES orcafacil.customer_success_health_scores(id),
  code varchar(80) NOT NULL, observed_value numeric(18,4) NOT NULL, contribution numeric(7,2) NOT NULL,
  weight numeric(7,2) NOT NULL, source varchar(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS orcafacil.customer_success_churn_risks (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, level varchar(20) NOT NULL,
  assessed_at timestamptz NOT NULL, source_fingerprint varchar(128) NOT NULL
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_churn_risk_factors (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, churn_risk_id uuid NOT NULL REFERENCES orcafacil.customer_success_churn_risks(id),
  code varchar(80) NOT NULL, points integer NOT NULL, reason text NOT NULL, source varchar(200) NOT NULL
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_retention_plans (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, owner_id uuid NOT NULL,
  reason text NOT NULL, objective text NOT NULL, status varchar(30) NOT NULL, result text, loss_reason text,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_retention_plan_items (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, plan_id uuid NOT NULL REFERENCES orcafacil.customer_success_retention_plans(id),
  owner_id uuid NOT NULL, description text NOT NULL, due_date date NOT NULL, completed_at timestamptz
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_expansion_opportunities (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, type varchar(60) NOT NULL,
  origin text NOT NULL, estimated_value numeric(18,2), crm_confirmation_at timestamptz, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_renewal_cycles (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, contract_id uuid NOT NULL,
  renewal_date date NOT NULL, stage varchar(20) NOT NULL, approved_at timestamptz, outcome_reason text
);
CREATE INDEX IF NOT EXISTS ix_cs_renewal_scope_date ON orcafacil.customer_success_renewal_cycles(account_id, renewal_date);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_nps_surveys (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, contact_id uuid NOT NULL,
  public_token_hash varchar(128) NOT NULL, expires_at timestamptz NOT NULL, answered_at timestamptz
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cs_nps_token ON orcafacil.customer_success_nps_surveys(public_token_hash);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_nps_responses (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, survey_id uuid NOT NULL REFERENCES orcafacil.customer_success_nps_surveys(id),
  score smallint NOT NULL CHECK(score BETWEEN 0 AND 10), comment text, answered_at timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cs_nps_response ON orcafacil.customer_success_nps_responses(survey_id);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_qbrs (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, owner_id uuid NOT NULL,
  period_start date NOT NULL, period_end date NOT NULL, executive_summary text NOT NULL,
  internal_notes text, sensitive_revenue numeric(18,2), next_review date, CHECK(period_end >= period_start)
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_playbooks (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(160) NOT NULL, current_version integer NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_playbook_versions (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, playbook_id uuid NOT NULL REFERENCES orcafacil.customer_success_playbooks(id),
  version integer NOT NULL, published_at timestamptz NOT NULL, steps_json jsonb NOT NULL,
  UNIQUE(playbook_id, version)
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_playbook_runs (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL,
  playbook_version_id uuid NOT NULL REFERENCES orcafacil.customer_success_playbook_versions(id),
  steps_snapshot_json jsonb NOT NULL, started_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_touchpoints (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, owner_id uuid NOT NULL,
  type varchar(40) NOT NULL, notes text NOT NULL, occurred_at timestamptz NOT NULL, next_action_at timestamptz
);
CREATE TABLE IF NOT EXISTS orcafacil.customer_success_alerts (
  id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, rule_code varchar(80) NOT NULL,
  reason text NOT NULL, source_url text NOT NULL, acknowledged_at timestamptz, resolved_at timestamptz
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_cs_alert_open ON orcafacil.customer_success_alerts(account_id, client_id, rule_code) WHERE resolved_at IS NULL;
