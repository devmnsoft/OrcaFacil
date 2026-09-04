using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class HomologationV63Tests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact]
    public void Every_V63_Gate_Is_Registered_In_The_Npm_Pipeline()
    {
        var package = File.ReadAllText(Path.Combine(Root, "package.json"));
        foreach (var gate in new[]
        {
            "homologation-v63", "critical-routes-v63", "profile-permissions-v63", "commercial-flow-v63",
            "operational-flow-v63", "financial-fiscal-flow-v63", "portal-flow-v63", "schema-drift-v63",
            "system-health-v63", "ui-total-v63", "design-system-v63"
        })
            Assert.Contains($"check:{gate}", package, StringComparison.Ordinal);
    }

    [Fact]
    public void Critical_Post_Handlers_Keep_Backend_Authorization_And_Tenant_Scope()
    {
        AssertProtectedAndScoped("src/OrcaFacil.Web/Pages/WorkOrders/Details.cshtml.cs");
        AssertProtectedAndScoped("src/OrcaFacil.Web/Pages/Payments/Index.cshtml.cs");
    }

    [Fact]
    public void Commercial_Journey_Keeps_Validation_And_Accessible_Confirmation()
    {
        var budget = Read("src/OrcaFacil.Web/Pages/Documents/CreateBudget.cshtml");
        var routine = Read("src/OrcaFacil.Web/Pages/CommercialRoutine/Index.cshtml");
        Assert.Contains("asp-validation-summary", budget, StringComparison.Ordinal);
        Assert.Contains("of-wizard-steps", budget, StringComparison.Ordinal);
        Assert.Contains("data-confirm", routine, StringComparison.Ordinal);
        Assert.Contains("AntiForgeryToken", routine, StringComparison.Ordinal);
    }

    private static void AssertProtectedAndScoped(string path)
    {
        var source = Read(path);
        Assert.Contains("[Authorize", source, StringComparison.Ordinal);
        Assert.Contains("AccountId", source, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));
}
