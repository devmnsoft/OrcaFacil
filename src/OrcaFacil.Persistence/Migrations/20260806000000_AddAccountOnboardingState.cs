using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace OrcaFacil.Persistence.Migrations;
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260806000000_AddAccountOnboardingState")]
public partial class AddAccountOnboardingState : Migration
{
 protected override void Up(MigrationBuilder m) { m.CreateTable("account_onboarding_states","orcafacil",t=>new{id=t.Column<Guid>(nullable:false),account_id=t.Column<Guid>(nullable:false),user_id=t.Column<Guid>(nullable:false),current_step=t.Column<string>(maxLength:32,nullable:false),business_profile_completed_at=t.Column<DateTime>(nullable:true),issuer_profile_completed_at=t.Column<DateTime>(nullable:true),first_client_completed_at=t.Column<DateTime>(nullable:true),first_service_completed_at=t.Column<DateTime>(nullable:true),first_budget_started_at=t.Column<DateTime>(nullable:true),first_budget_completed_at=t.Column<DateTime>(nullable:true),completed_at=t.Column<DateTime>(nullable:true),skipped_at=t.Column<DateTime>(nullable:true),last_seen_at=t.Column<DateTime>(nullable:false),created_at=t.Column<DateTime>(nullable:false),updated_at=t.Column<DateTime>(nullable:true),is_deleted=t.Column<bool>(nullable:false)},c=>c.PrimaryKey("pk_account_onboarding_states",x=>x.id)); m.CreateIndex("ix_account_onboarding_states_account_id_user_id","orcafacil","account_onboarding_states",new[]{"account_id","user_id"},unique:true); m.CreateIndex("ix_account_onboarding_states_current_step_last_seen_at","orcafacil","account_onboarding_states",new[]{"current_step","last_seen_at"}); }
 protected override void Down(MigrationBuilder m)=>m.DropTable("account_onboarding_states","orcafacil");
}
