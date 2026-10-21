using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations
{
    /// <summary>
    /// Migração aditiva e reversível: cria somente orcafacil.account_invitations
    /// (convites de equipe por e-mail). O EF detectou drift pré-existente de
    /// tabelas criadas/alteradas por SQL cru (budget_templates, manual_payments,
    /// ck_users_plan); a realidade dessas estruturas é gerida pelo
    /// database/script_completop.sql e pelos hotfixes idempotentes — nada disso
    /// entra aqui, para não alterar bases de produção fora do fluxo oficial.
    /// </summary>
    public partial class AddAccountInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_invitations",
                schema: "orcafacil",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    full_name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    role_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    used_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_invitations_business_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "orcafacil",
                        principalTable: "business_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_invitations_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalSchema: "orcafacil",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_account_id",
                schema: "orcafacil",
                table: "account_invitations",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_account_id_email",
                schema: "orcafacil",
                table: "account_invitations",
                columns: new[] { "account_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_created_at",
                schema: "orcafacil",
                table: "account_invitations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_expires_at",
                schema: "orcafacil",
                table: "account_invitations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_invited_by_user_id",
                schema: "orcafacil",
                table: "account_invitations",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_token_hash",
                schema: "orcafacil",
                table: "account_invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_invitations_used_at",
                schema: "orcafacil",
                table: "account_invitations",
                column: "used_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_invitations",
                schema: "orcafacil");
        }
    }
}
