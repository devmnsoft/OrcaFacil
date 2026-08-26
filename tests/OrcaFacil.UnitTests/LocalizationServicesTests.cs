using OrcaFacil.Application.Localization;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class LocalizationServicesTests
{
    [Fact]
    public void Invalid_locale_falls_back_to_pt_br()
    {
        var result = new LocalePreferenceService().Resolve(new("xx-ZZ", "xx-ZZ", "XXX", "Missing/Zone"), null, null);
        Assert.Equal("pt-BR", result.LanguageCode);
        Assert.Equal("BRL", result.CurrencyCode);
        Assert.Equal("UTC", result.TimeZoneId);
    }

    [Fact]
    public void Currency_formatting_does_not_convert_amount()
    {
        var service = new RegionalFormatService();
        Assert.Contains("1.234,56", service.FormatCurrency(1234.56m, "pt-BR", "BRL"));
        Assert.Contains("1,234.56", service.FormatCurrency(1234.56m, "en-US", "USD"));
    }

    [Fact]
    public void Import_is_preview_only_and_rejects_script()
    {
        var service = new TranslationImportService();
        var preview = service.PreviewJson("""[{"Key":"Common.Save","Value":"Save","LanguageCode":"en-US","Module":"Common","IsPublished":true},{"Key":"Common.Save","Value":"<script>x</script>","LanguageCode":"es-ES","Module":"Common","IsPublished":false}]""",
            new HashSet<string> { "Common.Save" }, new HashSet<string> { "en-US:Common.Save" });
        Assert.Single(preview.Valid);
        Assert.False(preview.Valid[0].IsPublished);
        Assert.Single(preview.Errors);
        Assert.Equal(1, preview.Conflicts);
    }

    [Fact]
    public void Hreflang_excludes_private_and_draft_routes()
    {
        var links = new HreflangService().Build(new Uri("https://orcafacil.example"),
        [
            new("/recursos", "pt-BR", "Recursos", "recursos", true),
            new("/admin/users", "en-US", "Users", "users", true),
            new("/features", "es-ES", "Funciones", "funciones", false)
        ]);
        Assert.Single(links);
        Assert.True(links.ContainsKey("pt-BR"));
    }
}
