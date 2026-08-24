using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddCrmV22Core : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS orcafacil.client_relationship_profiles (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, status varchar(24) NOT NULL, status_reason varchar(500) NOT NULL, commercial_temperature varchar(16) NOT NULL, last_interaction_at timestamptz NULL, next_action_at timestamptz NULL, commercial_owner_user_id uuid NULL, success_owner_user_id uuid NULL, source varchar(120) NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_crm_profile_account_client ON orcafacil.client_relationship_profiles(account_id, client_id);
CREATE TABLE IF NOT EXISTS orcafacil.client_interactions (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, user_id uuid NOT NULL, interaction_type varchar(24) NOT NULL, title varchar(180) NOT NULL, description varchar(5000) NOT NULL, interaction_date timestamptz NOT NULL, next_action_date timestamptz NULL, outcome varchar(1000) NULL, restricted_visibility boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_client_interactions_timeline ON orcafacil.client_interactions(account_id, client_id, interaction_date DESC);
CREATE TABLE IF NOT EXISTS orcafacil.client_health_scores (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, score integer NOT NULL CHECK(score BETWEEN 0 AND 100), classification varchar(32) NOT NULL, explanation_json jsonb NOT NULL, calculated_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_health_account_client ON orcafacil.client_health_scores(account_id, client_id, calculated_at DESC);
CREATE TABLE IF NOT EXISTS orcafacil.communication_opt_outs (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, channel varchar(32) NULL, commercial_communications boolean NOT NULL DEFAULT true, reason varchar(500) NOT NULL, opted_out_at timestamptz NOT NULL, registered_by_user_id uuid NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_opt_out_account_client_channel ON orcafacil.communication_opt_outs(account_id, client_id, channel);
CREATE TABLE IF NOT EXISTS orcafacil.nps_responses (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, survey_id uuid NOT NULL, score integer NOT NULL CHECK(score BETWEEN 0 AND 10), comment varchar(3000) NULL, answered_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_nps_survey_client ON orcafacil.nps_responses(account_id, survey_id, client_id);
CREATE TABLE IF NOT EXISTS orcafacil.retention_risk_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, factor_code varchar(80) NOT NULL, level varchar(16) NOT NULL, reason varchar(500) NOT NULL, recommended_action varchar(500) NOT NULL, detected_at timestamptz NOT NULL, resolved_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_retention_open ON orcafacil.retention_risk_events(account_id, client_id, factor_code) WHERE resolved_at IS NULL AND is_deleted = false;
CREATE TABLE IF NOT EXISTS orcafacil.crm_opportunities (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, kind varchar(16) NOT NULL, reason varchar(1000) NOT NULL, next_action varchar(500) NOT NULL, next_action_at timestamptz NOT NULL, status varchar(16) NOT NULL, discard_reason varchar(500) NULL, converted_document_id uuid NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_opportunity_account_client_status ON orcafacil.crm_opportunities(account_id, client_id, status);
""");

    protected override void Down(MigrationBuilder migrationBuilder) { /* Data-preserving release: rollback is intentionally manual. */ }
}
