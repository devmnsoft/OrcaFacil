using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>Adds commercial fields (conditions, warranty, payment method, discounts) to budget templates.</summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261009140000_AddBudgetTemplateCommercialFieldsV72")]
public sealed class AddBudgetTemplateCommercialFieldsV72 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS conditions_text text;
            ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS warranty_text varchar(2000);
            ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS payment_method varchar(60);
            ALTER TABLE orcafacil.budget_templates ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE orcafacil.budget_template_items ADD COLUMN IF NOT EXISTS discount numeric(18,2) NOT NULL DEFAULT 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE orcafacil.budget_templates DROP COLUMN IF EXISTS conditions_text;
            ALTER TABLE orcafacil.budget_templates DROP COLUMN IF EXISTS warranty_text;
            ALTER TABLE orcafacil.budget_templates DROP COLUMN IF EXISTS payment_method;
            ALTER TABLE orcafacil.budget_templates DROP COLUMN IF EXISTS discount;
            ALTER TABLE orcafacil.budget_template_items DROP COLUMN IF EXISTS discount;
            """);
    }
}
