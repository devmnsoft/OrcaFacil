using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;
using OrcaFacil.Persistence;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// Guarda de provisionamento do catálogo de planos no PostgreSQL. Antes de
/// hotfix_plan_feature_catalog_v75.sql, os recursos work_orders.enabled,
/// payments.manual.enabled e receipts.operational.enabled eram exigidos pelo código
/// mas não existiam em orcafacil.features, e a matriz plan_feature_values estava vazia
/// fora da IA — o que negava Aprovação pública, Ordens de serviço e Pagamentos manuais
/// para todos os planos. Estes testes impedem a regressão.
/// </summary>
public sealed class PlanCatalogPostgresSeedTests
{
    [JourneyPostgresFact]
    public async Task Todo_recurso_declarado_existe_no_catalogo_do_banco()
    {
        await using var db = Open();
        var codes = await db.Database.SqlQueryRaw<string>(
            "select code from orcafacil.features where is_deleted = false and is_active = true").ToListAsync();

        var missing = PlanFeatureCodes.All.Except(codes, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Empty(missing);
    }

    [JourneyPostgresFact]
    public async Task Matriz_do_banco_confere_com_a_referencia_de_cada_plano()
    {
        await using var db = Open();
        var rows = (await db.Database.SqlQueryRaw<string>("""
            select p.code || '|' || f.code || '|' || coalesce(v.boolean_value::text, '') || '|' ||
                   coalesce(v.integer_value::text, '') || '|' || v.is_unlimited::text
              from orcafacil.plan_feature_values v
              join orcafacil.features f on f.id = v.feature_id
              join orcafacil.plan_versions pv on pv.id = v.plan_version_id
              join orcafacil.plans p on p.id = pv.plan_id
             where v.is_deleted = false and f.is_deleted = false and pv.is_deleted = false
               and pv.version_number = 1
            """).ToListAsync()).ToHashSet(StringComparer.Ordinal);

        foreach (var (plan, settings) in PlanCatalogDefinitions.Features)
        {
            foreach (var (code, setting) in settings)
            {
                var expected = string.Join('|',
                    plan, code,
                    setting.Enabled == true ? "true" : "false",
                    setting.Limit?.ToString() ?? string.Empty,
                    setting.IsUnlimited ? "true" : "false");

                Assert.Contains(expected, rows);
            }
        }
    }

    private static OrcaFacilDbContext Open()
    {
        var connection = Environment.GetEnvironmentVariable("ORCAFACIL_JOURNEY_CONNECTION")
            ?? throw new InvalidOperationException("Conexão ausente.");
        return new OrcaFacilDbContext(new DbContextOptionsBuilder<OrcaFacilDbContext>()
            .UseNpgsql(connection).Options);
    }
}
