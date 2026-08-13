using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class AddCommercialMessageTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "commercial_message_templates", schema: "orcafacil", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false), account_id = table.Column<Guid>(type: "uuid", nullable: true),
            code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false), name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
            channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false), subject = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
            body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false), is_active = table.Column<bool>(type: "boolean", nullable: false),
            is_system = table.Column<bool>(type: "boolean", nullable: false), created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
            created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), is_deleted = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_commercial_message_templates", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_commercial_message_templates_account_id_code", schema: "orcafacil", table: "commercial_message_templates", columns: new[] { "account_id", "code" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "commercial_message_templates", schema: "orcafacil");
}
