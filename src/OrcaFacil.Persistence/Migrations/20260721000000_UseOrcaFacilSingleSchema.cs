using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class UseOrcaFacilSingleSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("orcafacil");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
