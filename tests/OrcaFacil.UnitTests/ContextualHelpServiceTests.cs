using OrcaFacil.Web.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// E-3: o drawer de ajuda precisa abrir no guia da tela atual. Antes o código vinha apenas de
/// ViewData e podia apontar para um guia inexistente (ex.: "quotes"), fazendo toda página cair
/// na visão geral; a rota agora é a fonte de verdade.
/// </summary>
public sealed class ContextualHelpServiceTests
{
    private readonly ContextualHelpService _help = new();

    [Fact]
    public void Rota_exata_retorna_o_guia_da_pagina()
    {
        Assert.Equal("documents", _help.ForPath("/Documents/Index")?.Code);
        Assert.Equal("clients", _help.ForPath("/Clients/Index")?.Code);
        Assert.Equal("my-plan", _help.ForPath("/Subscription/Index")?.Code);
    }

    [Fact]
    public void Rota_de_detalhe_herda_o_guia_da_pagina_mae()
    {
        Assert.Equal("document.details", _help.ForPath("/Documents/Details/a0e20000-0000-4000-8000-000000000102")?.Code);
        Assert.Equal("receipt", _help.ForPath("/Receipts/Details/42")?.Code);
    }

    [Fact]
    public void Barra_final_e_caixa_nao_mudam_o_guia()
    {
        Assert.Equal("documents", _help.ForPath("/Documents/Index/")?.Code);
        Assert.Equal("clients", _help.ForPath("/clients/index")?.Code);
    }

    [Fact]
    public void Rota_desconhecida_nao_inventa_guia()
    {
        Assert.Null(_help.ForPath("/Nao/Existe"));
        Assert.Null(_help.ForPath(""));
        Assert.Null(_help.ForPath(null!));
    }

    [Fact]
    public void Prefixo_parcial_nao_casa_com_outra_rota()
    {
        // "/Documents/DetailsExtra" não é filho de "/Documents/Details".
        Assert.Null(_help.ForPath("/Documents/DetailsExtra"));
        Assert.Null(_help.ForPath("/Documents/IndexOf"));
    }

    [Fact]
    public void Guia_do_orcamento_e_resolvido_pela_rota_do_wizard()
    {
        // As cinco etapas compartilham a mesma rota; a primeira é o ponto de entrada padrão.
        var guide = _help.ForPath("/Documents/CreateBudget");
        Assert.NotNull(guide);
        Assert.StartsWith("budget.", guide!.Code);
    }

    [Fact]
    public void Todo_guia_tem_codigo_unico_e_rota_absoluta()
    {
        var all = _help.All();
        Assert.NotEmpty(all);
        Assert.Equal(all.Count, all.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(all, guide => Assert.StartsWith("/", guide.RelatedPage));
    }
}
