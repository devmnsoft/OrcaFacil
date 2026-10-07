using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261007010000_AiBudgetAssistantV67")]
public sealed class AiBudgetAssistantV67 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(AiBudgetAssistantV67).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("hotfix_ai_budget_assistant_v67.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Script SQL de IA V6.7 não incorporado.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Migração aditiva: o histórico de consumo e as sugestões revisadas permanecem.
    }
}
