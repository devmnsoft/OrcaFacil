using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrcaFacil.Persistence.Migrations;

/// <summary>
/// Active modules require permission codes that were never inserted.
/// Owner and Administrator receive only those missing codes. Module contracts stay unchanged.
/// </summary>
public partial class GrantJourneyModulePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO orcafacil.permissions (id, code, display_name, is_platform_permission, created_at, is_deleted)
        SELECT gen_random_uuid(), v.code, v.display_name, false, now(), false
        FROM (VALUES
            ('Dashboard.View', 'Ver a visão geral'),
            ('Clients.View', 'Ver clientes'),
            ('Documents.View', 'Ver orçamentos'),
            ('Documents.Create', 'Criar orçamentos'),
            ('WorkOrders.View', 'Ver ordens de serviço'),
            ('Schedule.View', 'Ver a agenda'),
            ('Finance.View', 'Ver o financeiro'),
            ('CommercialRoutine.View', 'Ver a rotina comercial')
        ) AS v(code, display_name)
        WHERE NOT EXISTS (
            SELECT 1 FROM orcafacil.permissions p
            WHERE lower(p.code) = lower(v.code) AND p.is_deleted = false
        );

        INSERT INTO orcafacil.role_permissions (id, role_id, permission_id, created_at, is_deleted)
        SELECT gen_random_uuid(), r.id, p.id, now(), false
        FROM orcafacil.roles r
        JOIN orcafacil.permissions p ON p.is_deleted = false AND p.is_platform_permission = false
            AND p.code IN (
                'Dashboard.View', 'Clients.View', 'Documents.View', 'Documents.Create',
                'WorkOrders.View', 'Schedule.View', 'Finance.View', 'CommercialRoutine.View'
            )
        WHERE r.code IN ('Owner', 'Administrator') AND r.is_deleted = false
          AND NOT EXISTS (
              SELECT 1 FROM orcafacil.role_permissions rp
              WHERE rp.role_id = r.id AND rp.permission_id = p.id AND rp.is_deleted = false
          );
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DELETE FROM orcafacil.role_permissions rp
        USING orcafacil.roles r, orcafacil.permissions p
        WHERE rp.role_id = r.id AND rp.permission_id = p.id
          AND r.code IN ('Owner', 'Administrator')
          AND p.code IN (
              'Dashboard.View', 'Clients.View', 'Documents.View', 'Documents.Create',
              'WorkOrders.View', 'Schedule.View', 'Finance.View', 'CommercialRoutine.View'
          );
        """);
}
