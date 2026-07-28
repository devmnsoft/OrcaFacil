using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class VisualTransformationTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Landing_CtaUsesLightSurfaceWithExplicitDarkText()
    {
        var view = Read("src/OrcaFacil.Web/Pages/Index.cshtml");
        var surfaces = Read("src/OrcaFacil.Web/wwwroot/css/surfaces.css");
        var components = Read("src/OrcaFacil.Web/wwwroot/css/components.css");

        Assert.Contains("Crie seu primeiro orçamento grátis.", view);
        Assert.Contains("of-action-card", view);
        Assert.Contains("color: var(--of-text-primary)", surfaces);
        Assert.Contains(".of-action-card h2", components);
    }

    [Fact]
    public void Registration_HasPfPjSwitchAndPasswordsHiddenInitially()
    {
        var view = Read("src/OrcaFacil.Web/Pages/Auth/Register.cshtml");
        Assert.Contains("value=\"Individual\"", view);
        Assert.Contains("value=\"Company\"", view);
        Assert.Equal(2, Count(view, "type=\"password\""));
        Assert.Equal(2, Count(view, "data-password-toggle"));
        Assert.Contains("aria-pressed=\"false\"", view);
    }

    [Fact]
    public void Support_SearchHasAccessibleStatusAndLocalImplementation()
    {
        var view = Read("src/OrcaFacil.Web/Pages/Support/Index.cshtml");
        var script = Read("src/OrcaFacil.Web/wwwroot/js/app.js");
        Assert.Contains("data-support-search", view);
        Assert.Contains("aria-live=\"polite\"", view);
        Assert.Contains("data-support-item", script);
    }

    private static string Read(string relative) => File.ReadAllText(Path.Combine(RepositoryRoot, relative));
    private static int Count(string value, string fragment) => value.Split(fragment).Length - 1;
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
