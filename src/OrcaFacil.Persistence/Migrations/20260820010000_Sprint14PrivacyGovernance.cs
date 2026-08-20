using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class Sprint14PrivacyGovernance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(MigrationSql);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: security and audit history must survive rollback.
    }

    private const string MigrationSql = """
-- Sprint 14 / V1.5: LGPD and security governance (idempotent, non-destructive)
CREATE TABLE IF NOT EXISTS orcafacil.privacy_consents (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NOT NULL, consent_type varchar(40) NOT NULL, version varchar(40) NOT NULL, accepted_at timestamptz NOT NULL, revoked_at timestamptz NULL, ip_address varchar(64) NOT NULL DEFAULT '', user_agent varchar(512) NOT NULL DEFAULT '', created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.data_export_jobs (id uuid PRIMARY KEY, account_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, scope text NOT NULL, format text NOT NULL, status text NOT NULL, requested_at timestamptz NOT NULL, completed_at timestamptz NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.data_retention_policies (id uuid PRIMARY KEY, account_id uuid NOT NULL, data_type text NOT NULL, retention_days integer NOT NULL CHECK (retention_days >= 0), action varchar(30) NOT NULL, is_active boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.data_retention_runs (id uuid PRIMARY KEY, account_id uuid NOT NULL, policy_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, is_simulation boolean NOT NULL, matched_records integer NOT NULL, affected_records integer NOT NULL, started_at timestamptz NOT NULL, completed_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.sensitive_data_access_logs (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NOT NULL, entity_type text NOT NULL, entity_id uuid NOT NULL, access_type text NOT NULL, reason text NOT NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, correlation_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.security_events (id uuid PRIMARY KEY, account_id uuid NULL, user_id uuid NULL, event_type text NOT NULL, outcome text NOT NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, correlation_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.session_records (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NOT NULL, session_hash varchar(128) NOT NULL, started_at timestamptz NOT NULL, last_seen_at timestamptz NOT NULL, expires_at timestamptz NOT NULL, revoked_at timestamptz NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.public_token_access_logs (id uuid PRIMARY KEY, account_id uuid NULL, token_type text NOT NULL, entity_id uuid NOT NULL, accessed_at timestamptz NOT NULL, ip_address varchar(64) NOT NULL, user_agent varchar(512) NOT NULL, outcome text NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.account_security_settings (id uuid PRIMARY KEY, account_id uuid NOT NULL, session_expiration_minutes integer NOT NULL, minimum_password_length integer NOT NULL, require_password_change boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.audit_export_jobs (id uuid PRIMARY KEY, account_id uuid NOT NULL, requested_by_user_id uuid NOT NULL, status text NOT NULL, requested_at timestamptz NOT NULL, completed_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
ALTER TABLE orcafacil.audit_logs ADD COLUMN IF NOT EXISTS summary text NOT NULL DEFAULT '';
ALTER TABLE orcafacil.audit_logs ADD COLUMN IF NOT EXISTS correlation_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE orcafacil.data_subject_requests ADD COLUMN IF NOT EXISTS client_id uuid NULL;
ALTER TABLE orcafacil.data_subject_requests ADD COLUMN IF NOT EXISTS resolution_notes text NULL;
ALTER TABLE orcafacil.data_subject_requests ADD COLUMN IF NOT EXISTS reviewed_at timestamptz NULL;
CREATE INDEX IF NOT EXISTS ix_privacy_consents_account_user ON orcafacil.privacy_consents(account_id, user_id, consent_type);
CREATE INDEX IF NOT EXISTS ix_data_export_jobs_account_created ON orcafacil.data_export_jobs(account_id, created_at);
CREATE UNIQUE INDEX IF NOT EXISTS ix_retention_policy_account_type ON orcafacil.data_retention_policies(account_id, data_type) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_sensitive_access_account_entity ON orcafacil.sensitive_data_access_logs(account_id, entity_type, entity_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_security_events_account_created ON orcafacil.security_events(account_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_sessions_account_user ON orcafacil.session_records(account_id, user_id, revoked_at);
CREATE INDEX IF NOT EXISTS ix_public_token_access_account_date ON orcafacil.public_token_access_logs(account_id, accessed_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ix_account_security_settings_account ON orcafacil.account_security_settings(account_id) WHERE is_deleted = false;
INSERT INTO orcafacil.permissions(code,display_name,is_platform_permission) SELECT code,code,false FROM unnest(ARRAY['Privacy.View','Privacy.Manage','Privacy.ExportData','Privacy.AnonymizeData','Privacy.ManageRetention','Audit.View','Audit.Export','SensitiveData.View','Security.ViewSessions','Security.ManageSessions','Security.ManageUsers','Tokens.Revoke','Files.DownloadPrivate','Files.ManageVisibility']) code ON CONFLICT(code) DO NOTHING;
INSERT INTO orcafacil.role_permissions(role_id,permission_id,created_at,is_deleted) SELECT r.id,p.id,now(),false FROM orcafacil.roles r CROSS JOIN orcafacil.permissions p WHERE r.code IN ('Owner','Administrator') AND p.code IN ('Privacy.View','Privacy.Manage','Privacy.ExportData','Privacy.AnonymizeData','Privacy.ManageRetention','Audit.View','Audit.Export','SensitiveData.View','Security.ViewSessions','Security.ManageSessions','Security.ManageUsers','Tokens.Revoke','Files.DownloadPrivate','Files.ManageVisibility') ON CONFLICT(role_id,permission_id) DO NOTHING;
""";
}
