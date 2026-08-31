using Microsoft.EntityFrameworkCore.Migrations;

namespace OrcaFacil.Persistence.Migrations;

public partial class AddJourneyRefinementV51 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO orcafacil.permissions(code, display_name, is_platform_permission)
        SELECT code, display_name, false FROM (VALUES
          ('JourneyRefinement.View', 'Visualizar refinamento de jornadas'),
          ('JourneyRefinement.Manage', 'Gerenciar refinamento de jornadas'),
          ('JourneyRefinement.CommercialView', 'Visualizar jornada comercial'),
          ('JourneyRefinement.OperationalView', 'Visualizar jornada operacional'),
          ('JourneyRefinement.FinancialView', 'Visualizar jornada financeira'),
          ('JourneyRefinement.FiscalView', 'Visualizar jornada fiscal'),
          ('JourneyRefinement.ProjectView', 'Visualizar jornada de projetos'),
          ('JourneyRefinement.PortalView', 'Visualizar jornadas dos portais'),
          ('JourneyRefinement.DesignView', 'Visualizar qualidade de UX'),
          ('JourneyRefinement.ResolveFindings', 'Resolver pendências de refinamento'),
          ('JourneyRefinement.ExportReports', 'Exportar relatórios de refinamento')
        ) permission(code, display_name)
        ON CONFLICT(code) DO NOTHING;

        INSERT INTO orcafacil.role_permissions(role_id, permission_id, created_at, is_deleted)
        SELECT role.id, permission.id, now(), false
        FROM orcafacil.roles role CROSS JOIN orcafacil.permissions permission
        WHERE role.code IN ('Owner', 'Administrator') AND permission.code LIKE 'JourneyRefinement.%'
        ON CONFLICT(role_id, permission_id) DO NOTHING;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
