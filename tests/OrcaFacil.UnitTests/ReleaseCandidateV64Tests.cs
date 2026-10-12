using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class ReleaseCandidateV64Tests
{
    [Theory]
    [InlineData("public_document_decisions")]
    [InlineData("work_orders")]
    [InlineData("manual_payments")]
    [InlineData("budget_template_items")]
    public void Diagnostics_Covers_Release_Candidate_Critical_Tables(string table) =>
        Assert.Contains(table, DatabaseDiagnosticsService.RequiredTables);

    [Fact]
    public void Guided_Start_Requires_Personal_Template_To_Belong_To_Current_Account()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "GuidedBudgetStartService.cs"));
        Assert.Contains("x.AccountId == accountId && (x.UserId == null || x.UserId == userId)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Demo_Seed_Is_Explicit_Development_Only_And_Idempotent()
    {
        var root = FindRepositoryRoot();
        var command = File.ReadAllText(Path.Combine(root, "scripts", "seed-demo-release-candidate.ps1"));
        var sql = File.ReadAllText(Path.Combine(root, "database", "seed_demo_release_candidate_v64.sql"));
        Assert.Contains("Development", command, StringComparison.Ordinal);
        Assert.Contains("DEMO_SEED_ENABLED", command, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE orcafacil.users", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GoLive_rollout_contains_critical_route_events_schema()
    {
        var root = FindRepositoryRoot();
        var sql = File.ReadAllText(Path.Combine(root, "database", "sprint32_go_live_v33.sql"));
        var repairMigration = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Migrations", "20261012103000_RepairCriticalRouteEventsSchema.cs"));

        Assert.Contains("CREATE TABLE IF NOT EXISTS orcafacil.critical_route_events", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("duration_milliseconds bigint NOT NULL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS orcafacil.critical_route_events", repairMigration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ix_critical_route_events_route_status_code_created_at", repairMigration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Locale_scalar_queries_project_value_alias()
    {
        var root = FindRepositoryRoot();
        var signIn = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Services", "CookieUserSignInService.cs"));

        Assert.Contains("""SELECT language_code AS "Value" FROM orcafacil.user_locale_preferences""", signIn, StringComparison.Ordinal);
        Assert.Contains("""SELECT language_code AS "Value" FROM orcafacil.account_locale_settings""", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("\"SELECT language_code FROM orcafacil.user_locale_preferences", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void Module_access_denial_allows_html_status_page()
    {
        var root = FindRepositoryRoot();
        var middleware = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Middleware", "ModuleAccessMiddleware.cs"));

        Assert.Contains("AcceptsHtml(context)", middleware, StringComparison.Ordinal);
        Assert.Contains("UseStatusCodePagesWithReExecute(\"/Error/{0}\")",
            File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Program.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("context.Response.ContentType = \"text/plain; charset=utf-8\";\r\n            await context.Response.WriteAsync", middleware, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
