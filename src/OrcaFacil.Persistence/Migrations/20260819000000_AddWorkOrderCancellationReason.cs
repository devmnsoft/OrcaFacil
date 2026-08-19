using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>Preserves the mandatory operational reason without removing or rewriting existing orders.</summary>
public partial class AddWorkOrderCancellationReason : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "cancellation_reason", schema: "orcafacil", table: "work_orders", type: "character varying(1000)", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "cancellation_reason", schema: "orcafacil", table: "work_orders");
    }
}
