using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260908010000_SaasEnterpriseMultiTenantModulesV66")]
public sealed class SaasEnterpriseMultiTenantModulesV66 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(SaasEnterpriseMultiTenantModulesV66).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("hotfix_saas_enterprise_modules_v66.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Script SQL SaaS V6.6 não incorporado.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Migração aditiva de produção: rollback destrutivo não é automatizado.
    }
}
