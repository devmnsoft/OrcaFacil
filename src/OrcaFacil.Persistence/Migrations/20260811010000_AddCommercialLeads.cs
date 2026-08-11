using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

public partial class AddCommercialLeads : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "commercial_leads", schema: "orcafacil", columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false), name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
            company_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true), email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true), phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            segment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true), monthly_budget_volume = table.Column<int>(type: "integer", nullable: true), message = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
            consent_accepted = table.Column<bool>(type: "boolean", nullable: false), source_page = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false), status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
            converted_account_id = table.Column<Guid>(type: "uuid", nullable: true), internal_notes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true), discard_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true), is_deleted = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table => { table.PrimaryKey("pk_commercial_leads", x => x.id); table.CheckConstraint("ck_commercial_leads_consent", "consent_accepted = true"); table.CheckConstraint("ck_commercial_leads_contact", "email IS NOT NULL OR phone IS NOT NULL"); table.ForeignKey("fk_commercial_leads_business_accounts_converted_account_id", x => x.converted_account_id, principalSchema: "orcafacil", principalTable: "business_accounts", principalColumn: "id", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.CreateIndex(name: "ix_commercial_leads_email", schema: "orcafacil", table: "commercial_leads", column: "email");
        migrationBuilder.CreateIndex(name: "ix_commercial_leads_status_created_at", schema: "orcafacil", table: "commercial_leads", columns: new[] { "status", "created_at" });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "commercial_leads", schema: "orcafacil");
}
