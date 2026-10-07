using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261007160000_AiApplyGovernanceV69")]
public sealed class AiApplyGovernanceV69 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(AiApplyGovernanceV69).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("hotfix_ai_apply_governance_v69.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Script SQL da V6.9 não incorporado.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Migração aditiva: colunas de aplicação, permissões e benefícios permanecem.
    }
}
