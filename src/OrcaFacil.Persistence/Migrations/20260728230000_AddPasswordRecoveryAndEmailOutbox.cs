using Microsoft.EntityFrameworkCore.Infrastructure; using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace OrcaFacil.Persistence.Migrations;
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260728230000_AddPasswordRecoveryAndEmailOutbox")]
public sealed class AddPasswordRecoveryAndEmailOutbox : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("""
 CREATE TABLE orcafacil.password_reset_tokens (id uuid PRIMARY KEY, user_id uuid NOT NULL REFERENCES orcafacil.users(id) ON DELETE RESTRICT, token_hash varchar(64) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, expires_at timestamptz NOT NULL, used_at timestamptz NULL, revoked_at timestamptz NULL, requested_correlation_id varchar(100) NOT NULL, requested_ip_hash varchar(64), user_agent_hash varchar(64), created_by varchar(60) NOT NULL, is_deleted boolean NOT NULL DEFAULT false);
 CREATE UNIQUE INDEX uq_password_reset_tokens_token_hash ON orcafacil.password_reset_tokens(token_hash);
 CREATE INDEX ix_password_reset_tokens_user_id ON orcafacil.password_reset_tokens(user_id); CREATE INDEX ix_password_reset_tokens_expires_at ON orcafacil.password_reset_tokens(expires_at); CREATE INDEX ix_password_reset_tokens_used_at ON orcafacil.password_reset_tokens(used_at); CREATE INDEX ix_password_reset_tokens_revoked_at ON orcafacil.password_reset_tokens(revoked_at); CREATE INDEX ix_password_reset_tokens_created_at ON orcafacil.password_reset_tokens(created_at);
 CREATE TABLE orcafacil.email_outbox_messages (id uuid PRIMARY KEY, template_code varchar(80) NOT NULL, recipient_hash varchar(64) NOT NULL, recipient_masked varchar(254) NOT NULL, protected_recipient text NOT NULL, protected_payload text, status varchar(20) NOT NULL, priority varchar(20) NOT NULL, attempts integer NOT NULL DEFAULT 0, next_attempt_at timestamptz NOT NULL, processing_started_at timestamptz, processing_instance_id varchar(100), created_at timestamptz NOT NULL, updated_at timestamptz, sent_at timestamptz, dead_lettered_at timestamptz, last_error_code varchar(80), correlation_id varchar(100) NOT NULL, idempotency_key varchar(160) NOT NULL, is_deleted boolean NOT NULL DEFAULT false);
 CREATE UNIQUE INDEX uq_email_outbox_idempotency_key ON orcafacil.email_outbox_messages(idempotency_key); CREATE INDEX ix_email_outbox_claim ON orcafacil.email_outbox_messages(status,next_attempt_at,priority); CREATE INDEX ix_email_outbox_recipient_hash ON orcafacil.email_outbox_messages(recipient_hash);
 """);
 protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("DROP TABLE IF EXISTS orcafacil.email_outbox_messages; DROP TABLE IF EXISTS orcafacil.password_reset_tokens;");
}
