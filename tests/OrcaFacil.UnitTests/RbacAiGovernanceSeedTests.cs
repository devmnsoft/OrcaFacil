using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// Seed de RBAC das permissões de IA e de margem. Antes desta correção, Ai.Suggest, Ai.UseCopilot,
/// Ai.UseRag, Ai.UseSemanticSearch, Ai.AnalyzeDocuments e Margins.View eram exigidos pelo código
/// (AiGovernance / orquestrador) mas não existiam em orcafacil.permissions, o que tornava copiloto,
/// busca semântica e análise de documentos inutilizáveis para todos os papéis.
///
/// A regra de papéis é derivada do catálogo existente, não inventada: os códigos Ai.* seguem
/// Ai.View e Margins.View segue Finance.View. Este teste compara com esses pares para impedir
/// que o seed volte a divergir.
/// </summary>
public sealed class RbacAiGovernanceSeedTests
{
    private static readonly string[] AiCodes =
        ["Ai.Suggest", "Ai.UseCopilot", "Ai.UseRag", "Ai.UseSemanticSearch", "Ai.AnalyzeDocuments"];

    [JourneyPostgresFact]
    public async Task Codigos_de_IA_e_margem_existem_no_catalogo()
    {
        await using var db = Open();
        var codes = await ScalarCodesAsync(db);
        Assert.Equal(
            AiCodes.Append("Margins.View").OrderBy(x => x, StringComparer.Ordinal),
            codes.OrderBy(x => x, StringComparer.Ordinal));
    }

    [JourneyPostgresFact]
    public async Task Codigos_de_IA_seguem_os_papeis_de_Ai_View()
    {
        await using var db = Open();
        var expected = await RoleSetAsync(db, "Ai.View");
        Assert.NotEmpty(expected);
        foreach (var code in AiCodes)
            Assert.Equal(expected, await RoleSetAsync(db, code));
    }

    [JourneyPostgresFact]
    public async Task Margens_seguem_os_papeis_de_Finance_View()
    {
        await using var db = Open();
        var expected = await RoleSetAsync(db, "Finance.View");
        Assert.NotEmpty(expected);
        Assert.Equal(expected, await RoleSetAsync(db, "Margins.View"));
    }

    [JourneyPostgresFact]
    public async Task Nenhuma_concessao_aponta_para_papel_ou_permissao_removida()
    {
        await using var db = Open();
        var broken = await db.Database.SqlQueryRaw<string>("""
            select p.code || ' -> ' || r.code
              from orcafacil.role_permissions rp
              join orcafacil.roles r on r.id = rp.role_id
              join orcafacil.permissions p on p.id = rp.permission_id
             where rp.is_deleted = false
               and (r.is_deleted = true or p.is_deleted = true)
               and p.code in ('Ai.Suggest','Ai.UseCopilot','Ai.UseRag','Ai.UseSemanticSearch','Ai.AnalyzeDocuments','Margins.View','Ai.View','Finance.View')
            """).ToListAsync();
        Assert.Empty(broken);
    }

    private static OrcaFacilDbContext Open()
    {
        var connection = Environment.GetEnvironmentVariable("ORCAFACIL_JOURNEY_CONNECTION")
            ?? throw new InvalidOperationException("Conexão ausente.");
        return new OrcaFacilDbContext(new DbContextOptionsBuilder<OrcaFacilDbContext>()
            .UseNpgsql(connection).Options);
    }

    private static Task<List<string>> ScalarCodesAsync(OrcaFacilDbContext db) => db.Database.SqlQueryRaw<string>("""
        select code from orcafacil.permissions
         where is_deleted = false
           and is_platform_permission = false
           and code in ('Ai.Suggest','Ai.UseCopilot','Ai.UseRag','Ai.UseSemanticSearch','Ai.AnalyzeDocuments','Margins.View')
        """).ToListAsync();

    private static async Task<List<string>> RoleSetAsync(OrcaFacilDbContext db, string permissionCode)
    {
        // Constante local controlada pelo teste; sem entrada de usuário.
        var sql = """
            select r.code
              from orcafacil.role_permissions rp
              join orcafacil.roles r on r.id = rp.role_id
              join orcafacil.permissions p on p.id = rp.permission_id
             where rp.is_deleted = false and r.is_deleted = false and p.is_deleted = false
               and p.code = '
            """ + permissionCode + "' order by r.code";
        var roles = await db.Database.SqlQueryRaw<string>(sql).ToListAsync();
        return roles.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }
}
