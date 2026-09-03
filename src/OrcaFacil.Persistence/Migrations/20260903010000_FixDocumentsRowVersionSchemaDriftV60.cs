using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>Adds the application-managed Document concurrency token without deleting restored data.</summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260903010000_FixDocumentsRowVersionSchemaDriftV60")]
public sealed class FixDocumentsRowVersionSchemaDriftV60 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE orcafacil.documents ADD COLUMN IF NOT EXISTS row_version bytea;
            UPDATE orcafacil.documents SET row_version = decode(replace(gen_random_uuid()::text, '-', ''), 'hex') WHERE row_version IS NULL;
            ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET NOT NULL;
            ALTER TABLE orcafacil.documents ALTER COLUMN row_version SET DEFAULT decode(replace(gen_random_uuid()::text, '-', ''), 'hex');
            """);
    }

    // A rollback must never remove a live concurrency token or restored business data.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
