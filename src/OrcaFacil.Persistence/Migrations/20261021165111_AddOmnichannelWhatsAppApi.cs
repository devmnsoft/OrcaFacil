using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOmnichannelWhatsAppApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "omnichannel_whatsapp_accounts",
                schema: "orcafacil",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    business_account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    protected_access_token = table.Column<string>(type: "text", nullable: true),
                    protected_app_secret = table.Column<string>(type: "text", nullable: true),
                    webhook_verify_token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_health_check_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_failure = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_omnichannel_whatsapp_accounts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_omnichannel_whatsapp_accounts_account_id",
                schema: "orcafacil",
                table: "omnichannel_whatsapp_accounts",
                column: "account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "omnichannel_whatsapp_accounts",
                schema: "orcafacil");
        }
    }
}
