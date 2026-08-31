using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

public partial class AddFunctionalQualityV46 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
INSERT INTO orcafacil.permissions(code, display_name, is_platform_permission)
SELECT code, display_name, false FROM (VALUES
 ('Quality.View', 'Visualizar central de qualidade'), ('Quality.Manage', 'Gerenciar central de qualidade'),
 ('Quality.SourceAuditView', 'Visualizar auditoria de código-fonte'), ('Quality.BusinessRulesView', 'Visualizar auditoria de regras de negócio'),
 ('Quality.ReadinessView', 'Visualizar prontidão dos módulos'), ('Quality.ResolveFindings', 'Resolver achados de qualidade'),
 ('Quality.ExportReports', 'Exportar relatórios de qualidade')) permission(code, display_name)
ON CONFLICT (code) DO UPDATE SET display_name = EXCLUDED.display_name;
INSERT INTO orcafacil.role_permissions(role_id, permission_id, created_at, is_deleted)
SELECT role.id, permission.id, now(), false FROM orcafacil.roles role CROSS JOIN orcafacil.permissions permission
WHERE role.code IN ('Owner', 'Administrator') AND permission.code LIKE 'Quality.%'
ON CONFLICT (role_id, permission_id) DO UPDATE SET is_deleted = false;
""");

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Permission assignments are retained to avoid destructive authorization changes on rollback.
    }
}
