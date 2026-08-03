using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260803010000_ReleaseOperational23SecurityPrivacy")]
public partial class ReleaseOperational23SecurityPrivacy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_at timestamptz NULL;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_expires_at timestamptz NULL;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS failed_login_attempts integer NOT NULL DEFAULT 0;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS locked_until timestamptz NULL;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_successful_login_at timestamptz NULL;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_failed_login_at timestamptz NULL;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_reset_reason varchar(500) NULL;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_by_user_id uuid NULL;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS legacy_unversioned_acceptance boolean NOT NULL DEFAULT false;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;
            ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
            UPDATE orcafacil.users SET role='SuperAdmin' WHERE role='SuperAdministrator';
            UPDATE orcafacil.users SET plan='Professional' WHERE plan IN ('Pro','PROFESSIONAL','Premium','Starter','Basic');
            UPDATE orcafacil.users SET plan='Free' WHERE plan='FREE';
            UPDATE orcafacil.users SET plan='Business' WHERE plan IN ('BUSINESS','Enterprise','Ultimate');
            ALTER TABLE orcafacil.users DROP CONSTRAINT IF EXISTS ck_users_role;
            ALTER TABLE orcafacil.users DROP CONSTRAINT IF EXISTS ck_users_plan;
            ALTER TABLE orcafacil.users ADD CONSTRAINT ck_users_role CHECK (role IN ('User','Admin','SuperAdmin'));
            ALTER TABLE orcafacil.users ADD CONSTRAINT ck_users_plan CHECK (plan IN ('Free','Professional','Business'));
            UPDATE orcafacil.subscriptions SET plan='Professional' WHERE plan IN ('Pro','PROFESSIONAL','Premium','Starter','Basic');
            UPDATE orcafacil.subscriptions SET plan='Free' WHERE plan='FREE';
            UPDATE orcafacil.subscriptions SET plan='Business' WHERE plan IN ('BUSINESS','Enterprise','Ultimate');
            UPDATE orcafacil.features SET code='public_approval.enabled' WHERE code='public_link.enabled';
            """);

        migrationBuilder.CreateTable(name: "legal_documents", schema: "orcafacil", columns: table => new
        {
            id = table.Column<Guid>(nullable: false), type = table.Column<string>(maxLength: 30, nullable: false), code = table.Column<string>(maxLength: 80, nullable: false), title = table.Column<string>(maxLength: 180, nullable: false), is_active = table.Column<bool>(nullable: false), created_at = table.Column<DateTime>(nullable: false), updated_at = table.Column<DateTime>(nullable: true), is_deleted = table.Column<bool>(nullable: false)
        }, constraints: table => table.PrimaryKey("pk_legal_documents", x => x.id));
        migrationBuilder.CreateTable(name: "legal_document_versions", schema: "orcafacil", columns: table => new
        {
            id = table.Column<Guid>(nullable: false), legal_document_id = table.Column<Guid>(nullable: false), version_code = table.Column<string>(maxLength: 40, nullable: false), content_html = table.Column<string>(nullable: false), content_hash = table.Column<string>(maxLength: 128, nullable: false), status = table.Column<string>(maxLength: 30, nullable: false), published_at = table.Column<DateTime>(nullable: true), effective_at = table.Column<DateTime>(nullable: true), requires_reacceptance = table.Column<bool>(nullable: false), created_by_user_id = table.Column<Guid>(nullable: true), approved_by_user_id = table.Column<Guid>(nullable: true), approved_at = table.Column<DateTime>(nullable: true), created_at = table.Column<DateTime>(nullable: false), updated_at = table.Column<DateTime>(nullable: true), is_deleted = table.Column<bool>(nullable: false)
        }, constraints: table => { table.PrimaryKey("pk_legal_document_versions", x => x.id); table.ForeignKey("fk_legal_versions_documents", x => x.legal_document_id, "orcafacil", "legal_documents", "id", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.CreateTable(name: "legal_acceptances", schema: "orcafacil", columns: table => new
        {
            id=table.Column<Guid>(nullable:false), user_id=table.Column<Guid>(nullable:false), account_id=table.Column<Guid>(nullable:false), legal_document_version_id=table.Column<Guid>(nullable:false), accepted_at=table.Column<DateTime>(nullable:false), acceptance_source=table.Column<string>(maxLength:30,nullable:false), ip_hash=table.Column<string>(maxLength:128,nullable:false), user_agent_hash=table.Column<string>(maxLength:128,nullable:false), correlation_id=table.Column<Guid>(nullable:false), revoked_at=table.Column<DateTime>(nullable:true), revocation_reason=table.Column<string>(nullable:true), created_at=table.Column<DateTime>(nullable:false), updated_at=table.Column<DateTime>(nullable:true), is_deleted=table.Column<bool>(nullable:false)
        }, constraints: table => table.PrimaryKey("pk_legal_acceptances", x => x.id));
        migrationBuilder.CreateTable(name:"communication_consents",schema:"orcafacil",columns:table=>new{id=table.Column<Guid>(nullable:false),user_id=table.Column<Guid>(nullable:false),channel=table.Column<string>(maxLength:20,nullable:false),purpose=table.Column<string>(maxLength:30,nullable:false),granted=table.Column<bool>(nullable:false),granted_at=table.Column<DateTime>(nullable:true),revoked_at=table.Column<DateTime>(nullable:true),source=table.Column<string>(nullable:false),legal_text_version=table.Column<string>(nullable:false),ip_hash=table.Column<string>(nullable:false),correlation_id=table.Column<Guid>(nullable:false),created_at=table.Column<DateTime>(nullable:false),updated_at=table.Column<DateTime>(nullable:true),is_deleted=table.Column<bool>(nullable:false)},constraints:table=>table.PrimaryKey("pk_communication_consents",x=>x.id));
        migrationBuilder.CreateTable(name:"data_subject_requests",schema:"orcafacil",columns:table=>new{id=table.Column<Guid>(nullable:false),requester_user_id=table.Column<Guid>(nullable:false),account_id=table.Column<Guid>(nullable:false),type=table.Column<string>(maxLength:40,nullable:false),description=table.Column<string>(maxLength:4000,nullable:false),status=table.Column<string>(maxLength:40,nullable:false),requested_at=table.Column<DateTime>(nullable:false),verified_at=table.Column<DateTime>(nullable:true),due_at=table.Column<DateTime>(nullable:false),completed_at=table.Column<DateTime>(nullable:true),rejected_at=table.Column<DateTime>(nullable:true),rejection_reason=table.Column<string>(nullable:true),assigned_to_user_id=table.Column<Guid>(nullable:true),correlation_id=table.Column<Guid>(nullable:false),delivery_file_id=table.Column<Guid>(nullable:true),created_at=table.Column<DateTime>(nullable:false),updated_at=table.Column<DateTime>(nullable:true),is_deleted=table.Column<bool>(nullable:false)},constraints:table=>table.PrimaryKey("pk_data_subject_requests",x=>x.id));
        migrationBuilder.CreateTable(name:"privacy_vendors",schema:"orcafacil",columns:table=>new{id=table.Column<Guid>(nullable:false),name=table.Column<string>(maxLength:180,nullable:false),purpose=table.Column<string>(nullable:false),data_categories=table.Column<string>(nullable:false),country=table.Column<string>(nullable:false),privacy_url=table.Column<string>(nullable:false),contract_status=table.Column<string>(nullable:false),is_active=table.Column<bool>(nullable:false),last_reviewed_at=table.Column<DateTime>(nullable:true),created_at=table.Column<DateTime>(nullable:false),updated_at=table.Column<DateTime>(nullable:true),is_deleted=table.Column<bool>(nullable:false)},constraints:table=>table.PrimaryKey("pk_privacy_vendors",x=>x.id));
        migrationBuilder.CreateTable(name:"privacy_processing_activities",schema:"orcafacil",columns:table=>new{id=table.Column<Guid>(nullable:false),name=table.Column<string>(maxLength:180,nullable:false),data_categories=table.Column<string>(nullable:false),data_subjects=table.Column<string>(nullable:false),purpose=table.Column<string>(nullable:false),legal_basis=table.Column<string>(nullable:false),systems=table.Column<string>(nullable:false),recipients=table.Column<string>(nullable:false),international_transfer=table.Column<string>(nullable:true),retention_rule=table.Column<string>(nullable:false),security_controls=table.Column<string>(nullable:false),owner=table.Column<string>(nullable:false),status=table.Column<string>(maxLength:30,nullable:false),created_at=table.Column<DateTime>(nullable:false),updated_at=table.Column<DateTime>(nullable:true),is_deleted=table.Column<bool>(nullable:false)},constraints:table=>table.PrimaryKey("pk_privacy_processing_activities",x=>x.id));
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "privacy_processing_activities", "privacy_vendors", "data_subject_requests", "communication_consents", "legal_acceptances", "legal_document_versions", "legal_documents" }) migrationBuilder.DropTable(table, "orcafacil");
    }
}
