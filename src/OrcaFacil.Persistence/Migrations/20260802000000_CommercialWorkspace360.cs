using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260802000000_CommercialWorkspace360")]
public sealed class CommercialWorkspace360 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE orcafacil.clients ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true, ADD COLUMN IF NOT EXISTS is_favorite boolean NOT NULL DEFAULT false, ADD COLUMN IF NOT EXISTS internal_notes varchar(2000), ADD COLUMN IF NOT EXISTS last_interaction_at timestamptz, ADD COLUMN IF NOT EXISTS next_follow_up_at timestamptz, ADD COLUMN IF NOT EXISTS preferred_contact_channel varchar(24), ADD COLUMN IF NOT EXISTS version xid;
CREATE TABLE IF NOT EXISTS orcafacil.client_contacts (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, name varchar(160) NOT NULL, contact_type varchar(16) NOT NULL, value varchar(254) NOT NULL, label varchar(60), is_primary boolean NOT NULL DEFAULT false, receives_quotes boolean NOT NULL DEFAULT false, receives_receipts boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, sort_order integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_client_primary_contact ON orcafacil.client_contacts(account_id,client_id) WHERE is_primary AND NOT is_deleted;
CREATE TABLE IF NOT EXISTS orcafacil.client_tags (id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(60) NOT NULL, normalized_name varchar(60) NOT NULL, color_token varchar(32) NOT NULL DEFAULT 'accent', is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_client_tags_account_name ON orcafacil.client_tags(account_id,normalized_name);
CREATE TABLE IF NOT EXISTS orcafacil.client_tag_assignments (account_id uuid NOT NULL, client_id uuid NOT NULL, client_tag_id uuid NOT NULL, PRIMARY KEY(account_id,client_id,client_tag_id));
CREATE TABLE IF NOT EXISTS orcafacil.client_notes (id uuid PRIMARY KEY, account_id uuid NOT NULL, client_id uuid NOT NULL, content varchar(4000) NOT NULL, is_pinned boolean NOT NULL DEFAULT false, created_by_user_id uuid NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS orcafacil.service_categories (id uuid PRIMARY KEY, account_id uuid NOT NULL, name varchar(80) NOT NULL, normalized_name varchar(80) NOT NULL, description varchar(500), icon_name varchar(40) NOT NULL DEFAULT 'service', sort_order integer NOT NULL DEFAULT 0, is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_service_categories_account_name ON orcafacil.service_categories(account_id,normalized_name);
CREATE TABLE IF NOT EXISTS orcafacil.service_catalog_items (id uuid PRIMARY KEY, account_id uuid NOT NULL, code varchar(40), name varchar(180) NOT NULL, description varchar(1200), category_id uuid, unit_code varchar(24) NOT NULL, standard_price numeric(18,2) NOT NULL, estimated_cost numeric(18,2) NOT NULL, suggested_duration_minutes integer, internal_notes varchar(2000), is_favorite boolean NOT NULL DEFAULT false, is_active boolean NOT NULL DEFAULT true, use_count integer NOT NULL DEFAULT 0, last_used_at timestamptz, version xid, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE UNIQUE INDEX IF NOT EXISTS ux_service_catalog_account_code ON orcafacil.service_catalog_items(account_id,code) WHERE code IS NOT NULL AND NOT is_deleted;
CREATE TABLE IF NOT EXISTS orcafacil.service_price_history (id uuid PRIMARY KEY, account_id uuid NOT NULL, service_catalog_item_id uuid NOT NULL, previous_price numeric(18,2) NOT NULL, new_price numeric(18,2) NOT NULL, previous_cost numeric(18,2) NOT NULL, new_cost numeric(18,2) NOT NULL, reason varchar(500), changed_by_user_id uuid NOT NULL, changed_at timestamptz NOT NULL, created_at timestamptz NOT NULL, updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
""");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
DROP TABLE IF EXISTS orcafacil.service_price_history, orcafacil.service_catalog_items, orcafacil.service_categories, orcafacil.client_notes, orcafacil.client_tag_assignments, orcafacil.client_tags, orcafacil.client_contacts;
ALTER TABLE orcafacil.clients DROP COLUMN IF EXISTS version, DROP COLUMN IF EXISTS preferred_contact_channel, DROP COLUMN IF EXISTS next_follow_up_at, DROP COLUMN IF EXISTS last_interaction_at, DROP COLUMN IF EXISTS internal_notes, DROP COLUMN IF EXISTS is_favorite, DROP COLUMN IF EXISTS is_active;
""");
}
