using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260806000000_AddAccountOnboardingState")]
public partial class AddAccountOnboardingState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "account_onboarding_states",
            schema: "orcafacil",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                account_id = table.Column<Guid>(nullable: false),
                user_id = table.Column<Guid>(nullable: false),
                current_step = table.Column<string>(maxLength: 32, nullable: false),
                business_profile_completed_at = table.Column<DateTime>(nullable: true),
                issuer_profile_completed_at = table.Column<DateTime>(nullable: true),
                first_client_completed_at = table.Column<DateTime>(nullable: true),
                first_service_completed_at = table.Column<DateTime>(nullable: true),
                first_budget_started_at = table.Column<DateTime>(nullable: true),
                first_budget_completed_at = table.Column<DateTime>(nullable: true),
                completed_at = table.Column<DateTime>(nullable: true),
                skipped_at = table.Column<DateTime>(nullable: true),
                last_seen_at = table.Column<DateTime>(nullable: false),
                created_at = table.Column<DateTime>(nullable: false),
                updated_at = table.Column<DateTime>(nullable: true),
                is_deleted = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_account_onboarding_states", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_account_onboarding_states_account_id_user_id",
            schema: "orcafacil",
            table: "account_onboarding_states",
            columns: new[] { "account_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_account_onboarding_states_current_step_last_seen_at",
            schema: "orcafacil",
            table: "account_onboarding_states",
            columns: new[] { "current_step", "last_seen_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "account_onboarding_states",
            schema: "orcafacil");
    }
}
