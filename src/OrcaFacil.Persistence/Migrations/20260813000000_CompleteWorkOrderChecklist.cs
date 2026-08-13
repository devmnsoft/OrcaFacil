using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class CompleteWorkOrderChecklist : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "details", schema: "orcafacil", table: "work_order_checklist_items", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "is_required", schema: "orcafacil", table: "work_order_checklist_items", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.CreateIndex(name: "ix_work_order_checklist_items_required_pending", schema: "orcafacil", table: "work_order_checklist_items", columns: new[] { "account_id", "work_order_id", "is_required", "is_completed" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_work_order_checklist_items_required_pending", schema: "orcafacil", table: "work_order_checklist_items");
        migrationBuilder.DropColumn(name: "details", schema: "orcafacil", table: "work_order_checklist_items");
        migrationBuilder.DropColumn(name: "is_required", schema: "orcafacil", table: "work_order_checklist_items");
    }
}
