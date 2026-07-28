using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260729000000_AddCommercialJourney")]
public sealed class AddCommercialJourney : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE orcafacil.document_revisions (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL REFERENCES orcafacil.documents(id) ON DELETE RESTRICT, version_number integer NOT NULL, status varchar(32) NOT NULL, created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, sent_at timestamptz, snapshot_hash varchar(128) NOT NULL, protected_snapshot text NOT NULL, template_code varchar(40) NOT NULL, branding_snapshot jsonb NOT NULL DEFAULT '{}', total numeric(18,2) NOT NULL, valid_until timestamptz, is_current boolean NOT NULL, version xid NOT NULL DEFAULT '0', is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX ux_document_revisions_version ON orcafacil.document_revisions(account_id,document_id,version_number);
CREATE UNIQUE INDEX ux_document_revisions_current ON orcafacil.document_revisions(account_id,document_id,is_current) WHERE is_current = true;
CREATE INDEX ix_document_revisions_status_validity ON orcafacil.document_revisions(account_id,status,valid_until);
CREATE TABLE orcafacil.public_document_accesses (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, document_revision_id uuid NOT NULL REFERENCES orcafacil.document_revisions(id) ON DELETE RESTRICT, token_hash varchar(128) NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, expires_at timestamptz NOT NULL, revoked_at timestamptz, last_viewed_at timestamptz, view_count integer NOT NULL DEFAULT 0, status varchar(24) NOT NULL, created_by_user_id uuid NOT NULL, version xid NOT NULL DEFAULT '0', is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX ux_public_document_access_token_hash ON orcafacil.public_document_accesses(token_hash);
CREATE INDEX ix_public_document_access_document ON orcafacil.public_document_accesses(account_id,document_id,status);
CREATE INDEX ix_public_document_access_revision ON orcafacil.public_document_accesses(document_revision_id,status);
CREATE TABLE orcafacil.public_document_decisions (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, document_revision_id uuid NOT NULL, decision varchar(24) NOT NULL, customer_name varchar(180) NOT NULL, reason_code varchar(40), comment varchar(1000), created_at timestamptz NOT NULL, updated_at timestamptz, ip_hash varchar(128) NOT NULL, user_agent_hash varchar(128) NOT NULL, idempotency_key varchar(128) NOT NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX ux_public_decision_revision ON orcafacil.public_document_decisions(account_id,document_revision_id);
CREATE UNIQUE INDEX ux_public_decision_idempotency ON orcafacil.public_document_decisions(account_id,idempotency_key);
CREATE TABLE orcafacil.commercial_follow_ups (id uuid PRIMARY KEY, account_id uuid NOT NULL, document_id uuid NOT NULL, document_revision_id uuid, channel text NOT NULL, result text NOT NULL, occurred_at timestamptz NOT NULL, note varchar(1000), created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX ix_commercial_followups_document ON orcafacil.commercial_follow_ups(account_id,document_id,occurred_at);
CREATE TABLE orcafacil.work_orders (id uuid PRIMARY KEY, account_id uuid NOT NULL, source_document_id uuid, source_revision_id uuid, client_id uuid NOT NULL, number varchar(40) NOT NULL, title varchar(180) NOT NULL, description varchar(2000), status varchar(32) NOT NULL, scheduled_start timestamptz, scheduled_end timestamptz, started_at timestamptz, completed_at timestamptz, cancelled_at timestamptz, assigned_user_id uuid, address_snapshot jsonb NOT NULL DEFAULT '{}', client_snapshot jsonb NOT NULL DEFAULT '{}', items_snapshot jsonb NOT NULL DEFAULT '[]', total_snapshot numeric(18,2) NOT NULL, notes varchar(4000), created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, payment_received boolean NOT NULL DEFAULT false, payment_method varchar(80), version xid NOT NULL DEFAULT '0', is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX ux_work_orders_number ON orcafacil.work_orders(account_id,number);
CREATE UNIQUE INDEX ux_work_orders_revision ON orcafacil.work_orders(account_id,source_revision_id) WHERE source_revision_id IS NOT NULL;
CREATE INDEX ix_work_orders_schedule ON orcafacil.work_orders(account_id,status,scheduled_start);
CREATE INDEX ix_work_orders_assignee ON orcafacil.work_orders(account_id,assigned_user_id,scheduled_start);
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
DROP TABLE IF EXISTS orcafacil.work_orders;
DROP TABLE IF EXISTS orcafacil.commercial_follow_ups;
DROP TABLE IF EXISTS orcafacil.public_document_decisions;
DROP TABLE IF EXISTS orcafacil.public_document_accesses;
DROP TABLE IF EXISTS orcafacil.document_revisions;
""");
}
