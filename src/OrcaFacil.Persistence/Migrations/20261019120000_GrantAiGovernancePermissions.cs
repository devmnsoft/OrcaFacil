using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace OrcaFacil.Persistence.Migrations;

/// <summary>
/// Correção da deriva de seed de RBAC: códigos de permissão de IA e de margem são exigidos
/// pelo código (AiGovernance, orquestrador de IA) mas nunca foram inseridos em nenhuma migração,
/// deixando copiloto, busca semântica e análise de documentos inutilizáveis para todos os papéis.
/// Aditivo e idempotente. Não altera schema de negócio e não remove dados válidos.
///
/// O conjunto de papéis espelha o que já existe no catálogo:
/// - códigos <c>Ai.*</c> vão para os mesmos papéis que já têm <c>Ai.View</c> (Owner, Administrator,
///   Manager, Commercial);
/// - <c>Margins.View</c> (dado de custo) vai para os mesmos papéis que já têm <c>Finance.View</c>
///   (Owner, Administrator).
/// </summary>
[DbContext(typeof(OrcaFacilDbContext))]
[Migration("20261019120000_GrantAiGovernancePermissions")]
public sealed class GrantAiGovernancePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO orcafacil.permissions (id, code, display_name, is_platform_permission, created_at, is_deleted)
        SELECT gen_random_uuid(), v.code, v.display_name, false, now(), false
        FROM (VALUES
            ('Ai.Suggest', 'Sugerir melhorias com IA'),
            ('Ai.UseCopilot', 'Usar o copiloto de IA'),
            ('Ai.UseRag', 'Usar respostas com fontes de conhecimento'),
            ('Ai.UseSemanticSearch', 'Usar busca semântica'),
            ('Ai.AnalyzeDocuments', 'Analisar documentos com IA'),
            ('Margins.View', 'Ver custos e margens')
        ) AS v(code, display_name)
        ON CONFLICT (code) DO UPDATE
            SET display_name = EXCLUDED.display_name,
                is_deleted = false,
                updated_at = now();

        INSERT INTO orcafacil.role_permissions (id, role_id, permission_id, created_at, is_deleted)
        SELECT gen_random_uuid(), r.id, p.id, now(), false
        FROM orcafacil.roles r
        JOIN orcafacil.permissions p
            ON p.is_deleted = false
           AND p.is_platform_permission = false
           AND (
                (p.code IN ('Ai.Suggest', 'Ai.UseCopilot', 'Ai.UseRag', 'Ai.UseSemanticSearch', 'Ai.AnalyzeDocuments')
                    AND r.code IN ('Owner', 'Administrator', 'Manager', 'Commercial'))
             OR (p.code = 'Margins.View' AND r.code IN ('Owner', 'Administrator'))
           )
        WHERE r.is_deleted = false
        ON CONFLICT (role_id, permission_id) DO UPDATE
            SET is_deleted = false,
                updated_at = now();
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DELETE FROM orcafacil.role_permissions rp
        USING orcafacil.permissions p
        WHERE rp.permission_id = p.id
          AND p.code IN (
              'Ai.Suggest', 'Ai.UseCopilot', 'Ai.UseRag', 'Ai.UseSemanticSearch',
              'Ai.AnalyzeDocuments', 'Margins.View'
          );

        DELETE FROM orcafacil.permissions p
        WHERE p.code IN (
            'Ai.Suggest', 'Ai.UseCopilot', 'Ai.UseRag', 'Ai.UseSemanticSearch',
            'Ai.AnalyzeDocuments', 'Margins.View'
        );
        """);
}
