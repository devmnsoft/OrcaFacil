using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>Makes the waiting and overdue work-order states reachable: stores why the order waits,
/// since when, and when the background worker flagged it as overdue.</summary>
public partial class AddWorkOrderWaitAndOverdueState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "wait_reason", schema: "orcafacil", table: "work_orders", type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "None");
        migrationBuilder.AddColumn<DateTime>(name: "waiting_since", schema: "orcafacil", table: "work_orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "wait_note", schema: "orcafacil", table: "work_orders", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "overdue_marked_at", schema: "orcafacil", table: "work_orders", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "wait_reason", schema: "orcafacil", table: "work_orders");
        migrationBuilder.DropColumn(name: "waiting_since", schema: "orcafacil", table: "work_orders");
        migrationBuilder.DropColumn(name: "wait_note", schema: "orcafacil", table: "work_orders");
        migrationBuilder.DropColumn(name: "overdue_marked_at", schema: "orcafacil", table: "work_orders");
    }
}
