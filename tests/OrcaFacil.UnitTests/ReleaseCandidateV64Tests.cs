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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
