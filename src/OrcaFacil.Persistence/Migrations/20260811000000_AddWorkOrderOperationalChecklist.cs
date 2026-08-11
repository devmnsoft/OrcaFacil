using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class AddWorkOrderOperationalChecklist : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "work_order_checklist_items", schema: "orcafacil", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false), account_id = table.Column<Guid>(type: "uuid", nullable: false),
            work_order_id = table.Column<Guid>(type: "uuid", nullable: false), description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
            position = table.Column<int>(type: "integer", nullable: false), is_completed = table.Column<bool>(type: "boolean", nullable: false),
            completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            completion_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true), created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), is_deleted = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table => { table.PrimaryKey("pk_work_order_checklist_items", x => x.id); table.ForeignKey("fk_checklist_account", x => x.account_id, principalSchema: "orcafacil", principalTable: "business_accounts", principalColumn: "id", onDelete: ReferentialAction.Restrict); table.ForeignKey("fk_checklist_work_order", x => x.work_order_id, principalSchema: "orcafacil", principalTable: "work_orders", principalColumn: "id", onDelete: ReferentialAction.Cascade); table.ForeignKey("fk_checklist_user", x => x.completed_by_user_id, principalSchema: "orcafacil", principalTable: "users", principalColumn: "id", onDelete: ReferentialAction.SetNull); });
        migrationBuilder.CreateIndex(name: "ix_work_order_checklist_items_account_order_position", schema: "orcafacil", table: "work_order_checklist_items", columns: new[] { "account_id", "work_order_id", "position" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "work_order_checklist_items", schema: "orcafacil");
}
