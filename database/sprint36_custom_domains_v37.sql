BEGIN;
CREATE SCHEMA IF NOT EXISTS orcafacil;
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domains (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, host_name varchar(253) NOT NULL,
 normalized_host_name varchar(253) NOT NULL, domain_type integer NOT NULL, status integer NOT NULL,
 verification_token_hash varchar(64), verification_method integer NOT NULL DEFAULT 0,
 verified_at timestamptz, activated_at timestamptz, deactivated_at timestamptz,
 last_checked_at timestamptz, last_check_status varchar(80), created_by_user_id uuid NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_domains_normalized_host ON orcafacil.tenant_domains(normalized_host_name) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_tenant_domains_account_status ON orcafacil.tenant_domains(account_id,status);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domain_verifications (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, tenant_domain_id uuid NOT NULL REFERENCES orcafacil.tenant_domains(id),
 method integer NOT NULL, succeeded boolean NOT NULL, result_code varchar(80) NOT NULL,
 failure_reason varchar(1000), approved_by_user_id uuid, approval_reason varchar(1000),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_tenant_domain_verifications_history ON orcafacil.tenant_domain_verifications(account_id,tenant_domain_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domain_ssl_checks (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, tenant_domain_id uuid NOT NULL REFERENCES orcafacil.tenant_domains(id),
 status integer NOT NULL, certificate_expires_at timestamptz, failure_reason varchar(1000),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_email_domains (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, domain_name varchar(253) NOT NULL, status integer NOT NULL,
 spf_status integer NOT NULL, dkim_status integer NOT NULL, dmarc_status integer NOT NULL,
 verified_at timestamptz, last_checked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_tenant_email_domains_name ON orcafacil.tenant_email_domains(domain_name) WHERE is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.tenant_domain_audit_events (
 id uuid PRIMARY KEY, account_id uuid NOT NULL, tenant_domain_id uuid, user_id uuid,
 event_type varchar(100) NOT NULL, reason varchar(1000), correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_tenant_domain_audit_account_date ON orcafacil.tenant_domain_audit_events(account_id,created_at DESC);
COMMIT;
