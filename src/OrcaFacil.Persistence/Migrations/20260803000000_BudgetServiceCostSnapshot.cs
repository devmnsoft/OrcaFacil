using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260803000000_BudgetServiceCostSnapshot")]
public sealed class BudgetServiceCostSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS service_catalog_item_id uuid;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS estimated_cost_snapshot numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS category_snapshot varchar(80);
ALTER TABLE orcafacil.document_items ADD COLUMN IF NOT EXISTS duration_minutes_snapshot integer;
""");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
ALTER TABLE orcafacil.document_items DROP COLUMN IF EXISTS duration_minutes_snapshot;
ALTER TABLE orcafacil.document_items DROP COLUMN IF EXISTS category_snapshot;
ALTER TABLE orcafacil.document_items DROP COLUMN IF EXISTS estimated_cost_snapshot;
ALTER TABLE orcafacil.document_items DROP COLUMN IF EXISTS service_catalog_item_id;
""");
}
