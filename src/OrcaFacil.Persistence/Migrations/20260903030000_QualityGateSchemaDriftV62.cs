using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260903030000_QualityGateSchemaDriftV62")]
public sealed class QualityGateSchemaDriftV62 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS title varchar(160);
        ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS profession varchar(120);
        ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
        CREATE INDEX IF NOT EXISTS ix_budget_templates_account_active ON orcafacil.budget_templates(account_id, is_active) WHERE is_deleted = false;
        CREATE INDEX IF NOT EXISTS ix_budget_template_items_template ON orcafacil.budget_template_items(template_id);
        """);

    // Deliberately non-destructive: repaired production columns are retained.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
