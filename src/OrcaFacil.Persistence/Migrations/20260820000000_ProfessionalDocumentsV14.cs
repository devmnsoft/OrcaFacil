using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;
public partial class ProfessionalDocumentsV14 : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS orcafacil.file_assets (id uuid PRIMARY KEY, account_id uuid NOT NULL, uploaded_by_user_id uuid NOT NULL, original_file_name varchar(255) NOT NULL, stored_file_name varchar(80) NOT NULL, storage_path varchar(500) NOT NULL, content_type varchar(120) NOT NULL, extension varchar(12) NOT NULL, size_in_bytes bigint NOT NULL, sha256_hash varchar(64) NOT NULL, category integer NOT NULL, visibility integer NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_file_assets_account_created ON orcafacil.file_assets(account_id, created_at);
CREATE INDEX IF NOT EXISTS ix_file_assets_account_hash ON orcafacil.file_assets(account_id, sha256_hash);
CREATE TABLE IF NOT EXISTS orcafacil.file_asset_links (id uuid PRIMARY KEY, account_id uuid NOT NULL, file_asset_id uuid NOT NULL REFERENCES orcafacil.file_assets(id) ON DELETE RESTRICT, entity_type varchar(40) NOT NULL, entity_id uuid NOT NULL, visibility integer NOT NULL, created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_file_asset_links_entity ON orcafacil.file_asset_links(account_id, entity_type, entity_id);
CREATE TABLE IF NOT EXISTS orcafacil.company_branding_profiles (id uuid PRIMARY KEY, account_id uuid NOT NULL UNIQUE, logo_file_asset_id uuid NULL REFERENCES orcafacil.file_assets(id) ON DELETE SET NULL, trade_name varchar(160) NOT NULL, legal_name text NULL, document_number text NULL, phone text NULL, whats_app text NULL, commercial_email text NULL, website text NULL, address text NULL, primary_color varchar(7) NOT NULL, secondary_color varchar(7) NOT NULL, default_footer text NULL, default_commercial_notes text NULL, visual_signature text NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.document_templates (id uuid PRIMARY KEY, account_id uuid NULL, name varchar(160) NOT NULL, type integer NOT NULL, is_default boolean NOT NULL, is_active boolean NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_document_templates_default ON orcafacil.document_templates(account_id, type, is_default);
CREATE TABLE IF NOT EXISTS orcafacil.document_template_versions (id uuid PRIMARY KEY, template_id uuid NOT NULL REFERENCES orcafacil.document_templates(id) ON DELETE RESTRICT, version_number integer NOT NULL, content text NOT NULL, css_content text NULL, header_content text NULL, footer_content text NULL, variables_json jsonb NOT NULL DEFAULT '[]', published_at timestamptz NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(template_id, version_number));
CREATE TABLE IF NOT EXISTS orcafacil.document_audit_events (id uuid PRIMARY KEY, account_id uuid NOT NULL, user_id uuid NULL, event_type varchar(80) NOT NULL, entity_type varchar(40) NOT NULL, entity_id uuid NOT NULL, metadata_json jsonb NULL, created_at timestamptz NOT NULL, updated_at timestamptz NULL, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_document_audit_events_account_created ON orcafacil.document_audit_events(account_id, created_at);
""");
 protected override void Down(MigrationBuilder migrationBuilder) { }
}
