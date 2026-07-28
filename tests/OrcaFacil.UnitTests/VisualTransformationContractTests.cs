namespace OrcaFacil.UnitTests;

public sealed class VisualTransformationContractTests
{
    private static string RepoFile(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }

    [Fact]
    public void Landing_Cta_UsesExplicitLightSurface()
    {
        var view = RepoFile("src", "OrcaFacil.Web", "Pages", "Index.cshtml");
        Assert.Contains("of-final-cta of-surface-light", view);
        Assert.Contains("Crie seu primeiro orçamento grátis.", view);
    }

    [Fact]
    public void Registration_KeepsMarketingOptional_AndPasswordsHiddenInitially()
    {
        var view = RepoFile("src", "OrcaFacil.Web", "Pages", "Auth", "Register.cshtml");
        Assert.Contains("Input.AcceptMarketing", view);
        Assert.Contains("(opcional)", view);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(view, "type=\"password\"").Count);
    }

    [Fact]
    public void DesignSystem_DefinesAllRequiredSurfaceContracts()
    {
        var css = RepoFile("src", "OrcaFacil.Web", "wwwroot", "css", "surfaces.css");
        foreach (var surface in new[] { "page", "light", "soft", "dark", "brand", "success", "warning", "danger" })
            Assert.Contains($".of-surface-{surface}", css);
    }
}
