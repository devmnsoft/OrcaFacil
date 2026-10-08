using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261007225000_ClientRowVersionDefaultV70")]
public sealed class ClientRowVersionDefaultV70 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(ClientRowVersionDefaultV70).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("hotfix_client_rowversion_v70.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Script SQL da V7.0 não incorporado.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Aditiva: o default de clients.version permanece.
    }
}
