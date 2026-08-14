using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>
/// Repairs installations whose users table predates the complete authentication contract.
/// Every operation is additive and idempotent so the migration is also safe after the local SQL patch.
/// </summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20260813070000_AddUserLoginSecurityColumns")]
public partial class AddUserLoginSecurityColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_privacy_at timestamptz NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS accepted_terms_at timestamptz NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS block_reason varchar(500) NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS failed_login_attempts integer NOT NULL DEFAULT 0;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS is_blocked boolean NOT NULL DEFAULT false;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_failed_login_at timestamptz NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS last_successful_login_at timestamptz NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS legacy_unversioned_acceptance boolean NOT NULL DEFAULT false;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS locked_until timestamptz NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_at timestamptz NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_changed_by_user_id uuid NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_expires_at timestamptz NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS password_reset_reason varchar(500) NULL;
        ALTER TABLE orcafacil.users ADD COLUMN IF NOT EXISTS session_version integer NOT NULL DEFAULT 1;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately non-destructive: these columns may have existed before this repair migration
        // and contain authentication history. Rolling back the release must never erase that data.
    }
}
