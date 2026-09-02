using System.Text.RegularExpressions;
using Xunit;

namespace OrcaFacil.UnitTests;

internal static class Sprint55Source
{
    public static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}

public sealed class DesignSystemStructureTests
{
    [Fact] public void V56_DefinesSemanticTokensAndReusableComponents()
    {
        var tokens = Sprint55Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "tokens.css");
        var components = Sprint55Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "design-system.css");
        Assert.Contains("Design System V5.6", tokens);
        foreach (var token in new[] { "--of-shadow-md", "--of-transition-fast", "--of-icon-md", "--of-breakpoint-md" }) Assert.Contains(token, tokens);
        foreach (var component in new[] { "page-hero", "summary-card", "status-badge", "loading-state", "premium-table", "health-card" }) Assert.Contains(component, components);
    }
}

public sealed class NavigationQualityTests
{
    [Fact] public void PrimaryNavigation_HasNoDeadLinks()
    {
        var navigation = Sprint55Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "Partials", "_AuthenticatedNavigation.cshtml");
        Assert.DoesNotMatch(new Regex("href\\s*=\\s*[\\\"']#", RegexOptions.IgnoreCase), navigation);
    }
}

public sealed class DashboardDesignModelTests
{
    [Fact] public void Dashboard_UsesRealMetricsAndUsefulEmptyState()
    {
        var page = Sprint55Source.Read("src", "OrcaFacil.Web", "Pages", "Dashboard", "Index.cshtml");
        Assert.Contains("dashboard.TotalBudgets", page);
        Assert.Contains("dashboard.BudgetTotal", page);
        Assert.Contains("_EmptyState", page);
        Assert.DoesNotContain("Math.random", page, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class LoginPageStructureTests
{
    [Fact] public void Login_IsAccessibleAndPreservesSecurePost()
    {
        var page = Sprint55Source.Read("src", "OrcaFacil.Web", "Pages", "Auth", "Login.cshtml");
        Assert.Contains("<h1", page);
        Assert.Contains("AntiForgeryToken", page);
        Assert.Contains("data-loading-message", page);
        Assert.Contains("aria-live=\"assertive\"", page);
    }
}

public sealed class FormQualityTests
{
    [Fact] public void CriticalLoginButtonsDeclareTypeAndInputsHaveLabels()
    {
        var page = Sprint55Source.Read("src", "OrcaFacil.Web", "Pages", "Auth", "Login.cshtml");
        Assert.Equal(2, Regex.Matches(page, "<button\\b").Count);
        Assert.Equal(2, Regex.Matches(page, "<button\\b[^>]*\\btype=", RegexOptions.IgnoreCase).Count);
        Assert.Contains("label asp-for=\"Input.Email\"", page);
        Assert.Contains("label asp-for=\"Input.Password\"", page);
    }
}

public sealed class PortalVisibilityTests
{
    [Fact] public void ClientLayout_LoadsDedicatedPortalStylesWithoutSensitiveTerms()
    {
        var layout = Sprint55Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "_ClientLayout.cshtml");
        Assert.Contains("css/portals.css", layout);
        Assert.DoesNotContain("DRE", layout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("margem", layout, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class SystemHealthDesignTests
{
    [Fact] public void AdminHealth_IsActionableAndDoesNotRenderSecrets()
    {
        var page = Sprint55Source.Read("src", "OrcaFacil.Web", "Areas", "Admin", "Pages", "Dashboard.cshtml");
        Assert.Contains("Saúde", page);
        Assert.Contains("Banco", page);
        Assert.DoesNotContain("ConnectionString", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", page, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class AccessibilityBasicTests
{
    [Fact] public void MotionFocusAndLiveFeedbackContractsExist()
    {
        var baseCss = Sprint55Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "base.css");
        var design = Sprint55Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "design-system.css");
        var login = Sprint55Source.Read("src", "OrcaFacil.Web", "Pages", "Auth", "Login.cshtml");
        Assert.Contains(":focus-visible", baseCss);
        Assert.Contains("prefers-reduced-motion", design);
        Assert.Contains("aria-live", login);
    }
}
