using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class EnrichServiceCatalogPricing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS desired_margin_percentage numeric(5,2) NOT NULL DEFAULT 0;
        ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_delivery_term varchar(120);
        ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS default_notes varchar(2000);
        ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS tags varchar(500);
        ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS is_recurring boolean NOT NULL DEFAULT false;
        ALTER TABLE orcafacil.service_catalog_items ADD COLUMN IF NOT EXISTS is_recommended boolean NOT NULL DEFAULT false;
        CREATE INDEX IF NOT EXISTS ix_service_catalog_items_account_recurring ON orcafacil.service_catalog_items(account_id, is_recurring) WHERE is_deleted = false;
        CREATE INDEX IF NOT EXISTS ix_service_catalog_items_account_recommended ON orcafacil.service_catalog_items(account_id, is_recommended) WHERE is_deleted = false AND is_active = true;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP INDEX IF EXISTS orcafacil.ix_service_catalog_items_account_recommended;
        DROP INDEX IF EXISTS orcafacil.ix_service_catalog_items_account_recurring;
        ALTER TABLE orcafacil.service_catalog_items DROP COLUMN IF EXISTS is_recommended;
        ALTER TABLE orcafacil.service_catalog_items DROP COLUMN IF EXISTS is_recurring;
        ALTER TABLE orcafacil.service_catalog_items DROP COLUMN IF EXISTS tags;
        ALTER TABLE orcafacil.service_catalog_items DROP COLUMN IF EXISTS default_notes;
        ALTER TABLE orcafacil.service_catalog_items DROP COLUMN IF EXISTS default_delivery_term;
        ALTER TABLE orcafacil.service_catalog_items DROP COLUMN IF EXISTS desired_margin_percentage;
        """);
}
