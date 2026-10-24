using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Plans;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// Coerência do catálogo de planos. Antes desta correção, a matriz de recursos
/// (PlanCatalogDefinitions) ignorava códigos exigidos pelo código e o manifesto de
/// prontidão cobria apenas 6 dos 29 recursos. Estes testes travam a paridade entre
/// os códigos declarados, a matriz por plano e o manifesto de prontidão.
/// </summary>
public sealed class PlanCatalogParityTests
{
    [Fact]
    public void Todo_recurso_declarado_tem_nome_e_matriz_em_todos_os_planos()
    {
        foreach (var code in PlanFeatureCodes.All)
            Assert.True(PlanFeatureCodes.DisplayNames.ContainsKey(code), $"Recurso sem nome de exibição: {code}.");

        foreach (var plan in PlanCatalogDefinitions.Plans.Keys)
        {
            var matrix = PlanCatalogDefinitions.ForPlan(plan);
            Assert.Equal(PlanFeatureCodes.All.Count, matrix.Count);
            foreach (var code in PlanFeatureCodes.All)
                Assert.True(matrix.ContainsKey(code), $"O plano {plan} não define o recurso {code}.");
        }
    }

    [Fact]
    public void Manifesto_de_prontidao_cobre_exatamente_os_recursos_declarados()
    {
        var manifest = new PlanFeatureReadinessService().GetAll()
            .Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var expected = PlanFeatureCodes.All.OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, manifest);
    }

    [Fact]
    public void Aprovacao_publica_e_ordem_de_servico_diferem_entre_gratis_e_pago()
    {
        var free = PlanCatalogDefinitions.ForPlan("FREE");
        var professional = PlanCatalogDefinitions.ForPlan("PROFESSIONAL");

        Assert.False(free[PlanFeatureCodes.PublicApprovalEnabled].Enabled);
        Assert.False(free[PlanFeatureCodes.WorkOrdersEnabled].Enabled);
        Assert.True(professional[PlanFeatureCodes.PublicApprovalEnabled].Enabled);
        Assert.True(professional[PlanFeatureCodes.WorkOrdersEnabled].Enabled);
    }

    [Fact]
    public void Merge_usa_o_banco_como_precedencia_e_preenche_lacunas_com_a_matriz()
    {
        var stored = new Dictionary<string, PlanFeatureSetting>(StringComparer.Ordinal)
        {
            [PlanFeatureCodes.PdfMonthlyLimit] = new(true, 3, false),
            ["codigo.desconhecido"] = new(true)
        };

        var merged = PlanAccessService.MergeWithCatalog("FREE", stored);

        Assert.Equal(3, merged[PlanFeatureCodes.PdfMonthlyLimit].Limit);
        Assert.Equal(25, merged[PlanFeatureCodes.ClientsActiveLimit].Limit);
        Assert.True(merged["codigo.desconhecido"].Enabled);
    }

    [Fact]
    public void Merge_sem_plano_conhecido_retorna_o_banco_intacto()
    {
        IReadOnlyDictionary<string, PlanFeatureSetting> stored =
            new Dictionary<string, PlanFeatureSetting>(StringComparer.Ordinal) { ["x"] = new(true) };

        Assert.Same(stored, PlanAccessService.MergeWithCatalog("INEXISTENTE", stored));
    }

    [Fact]
    public void Publicacao_aceita_recursos_operacionais_e_recusa_desconhecidos()
    {
        var service = new PlanFeatureReadinessService();

        service.EnsurePublishable([PlanFeatureCodes.PublicApprovalEnabled, PlanFeatureCodes.TeamMembersLimit]);

        Assert.Throws<InvalidOperationException>(() => service.EnsurePublishable(["recurso.inexistente"]));
    }
}
