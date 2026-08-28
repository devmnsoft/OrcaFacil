using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddAssetOperationsV44 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(AddAssetOperationsV44).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("patch_sprint43_assets_v44.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Sprint 43 database patch was not embedded.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }
    protected override void Down(MigrationBuilder migrationBuilder) { /* Additive migration: operational history is never dropped. */ }
}
