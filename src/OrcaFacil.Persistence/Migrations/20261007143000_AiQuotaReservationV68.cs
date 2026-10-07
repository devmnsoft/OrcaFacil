using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261007143000_AiQuotaReservationV68")]
public sealed class AiQuotaReservationV68 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(AiQuotaReservationV68).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("hotfix_ai_quota_reservation_v68.sql", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Script SQL de cota V6.8 não incorporado.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Migração aditiva: reservas de cota e o índice de idempotência permanecem.
    }
}
