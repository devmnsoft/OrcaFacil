using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261007120000_BillingCoverageV67")]
public sealed class BillingCoverageV67 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(BillingCoverageV67).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("hotfix_billing_coverage_v67.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Script SQL de cobertura V6.7 não incorporado.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Migração aditiva: os vínculos de cobertura permanecem para auditoria.
    }
}
