using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class SaasOperationsV16 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Sql);
    protected override void Down(MigrationBuilder migrationBuilder) { }

    private const string Sql = """
CREATE TABLE IF NOT EXISTS orcafacil.background_jobs (id uuid PRIMARY KEY, name varchar(160) NOT NULL, schedule varchar(80) NOT NULL, is_enabled boolean NOT NULL DEFAULT true, next_execution_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_background_jobs_name ON orcafacil.background_jobs(name);
CREATE TABLE IF NOT EXISTS orcafacil.job_executions (id uuid PRIMARY KEY, job_name varchar(160) NOT NULL, execution_id varchar(160) NOT NULL, instance_id varchar(160) NOT NULL, status varchar(24) NOT NULL, started_at timestamptz NOT NULL, finished_at timestamptz NULL, duration_milliseconds bigint NULL, error_summary varchar(500) NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_job_executions_execution_id ON orcafacil.job_executions(execution_id);
CREATE INDEX IF NOT EXISTS ix_job_executions_job_started ON orcafacil.job_executions(job_name, started_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.job_locks (id uuid PRIMARY KEY, name varchar(160) NOT NULL, locked_by varchar(160) NULL, locked_until timestamptz NULL, acquired_at timestamptz NULL, released_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_job_locks_name ON orcafacil.job_locks(name);
CREATE TABLE IF NOT EXISTS orcafacil.processing_outbox (id uuid PRIMARY KEY, account_id uuid NOT NULL, type varchar(80) NOT NULL, idempotency_key varchar(200) NOT NULL, payload_json jsonb NOT NULL DEFAULT '{}', status varchar(24) NOT NULL, priority integer NOT NULL DEFAULT 0, attempts integer NOT NULL DEFAULT 0, maximum_attempts integer NOT NULL DEFAULT 5, next_attempt_at timestamptz NOT NULL, processing_started_at timestamptz NULL, processing_instance_id varchar(160) NULL, last_error varchar(500) NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_processing_outbox_account_key ON orcafacil.processing_outbox(account_id,idempotency_key);
CREATE INDEX IF NOT EXISTS ix_processing_outbox_ready ON orcafacil.processing_outbox(status,next_attempt_at,priority DESC) WHERE is_deleted=false;
CREATE TABLE IF NOT EXISTS orcafacil.system_metrics (id uuid PRIMARY KEY, account_id uuid NULL, name varchar(160) NOT NULL, value double precision NOT NULL, period_start timestamptz NOT NULL, period_end timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_system_metrics_scope_period ON orcafacil.system_metrics(account_id,name,period_start DESC);
CREATE TABLE IF NOT EXISTS orcafacil.slow_query_logs (id uuid PRIMARY KEY, account_id uuid NULL, user_id uuid NULL, route varchar(300) NOT NULL, operation varchar(160) NOT NULL, elapsed_milliseconds bigint NOT NULL, correlation_id varchar(160) NOT NULL, summary varchar(500) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_slow_query_logs_scope_created ON orcafacil.slow_query_logs(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.tenant_usage_metrics (id uuid PRIMARY KEY, account_id uuid NOT NULL, resource varchar(100) NOT NULL, used bigint NOT NULL, "limit" bigint NULL, period_start timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_tenant_usage_scope_resource_period ON orcafacil.tenant_usage_metrics(account_id,resource,period_start);
CREATE TABLE IF NOT EXISTS orcafacil.cache_invalidation_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, cache_region varchar(100) NOT NULL, reason varchar(300) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_cache_invalidations_scope_created ON orcafacil.cache_invalidation_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.quota_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, resource varchar(100) NOT NULL, used bigint NOT NULL, "limit" bigint NOT NULL, blocked boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_quota_events_scope_created ON orcafacil.quota_events(account_id,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.rate_limit_events (id uuid PRIMARY KEY, account_id uuid NULL, policy varchar(100) NOT NULL, client_fingerprint varchar(128) NOT NULL, route varchar(300) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_rate_limit_events_policy_created ON orcafacil.rate_limit_events(policy,created_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.worker_heartbeats (id uuid PRIMARY KEY, instance_id varchar(160) NOT NULL, last_seen_at timestamptz NOT NULL, status varchar(40) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ix_worker_heartbeats_instance ON orcafacil.worker_heartbeats(instance_id);
""";
}
