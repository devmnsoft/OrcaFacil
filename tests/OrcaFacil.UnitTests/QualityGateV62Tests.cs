using OrcaFacil.Application.Quality;
using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class QualityGateServiceTests
{
    [Fact]
    public void Score_Is_Derived_Only_From_Executed_Rules()
    {
        var snapshot = new QualityGateSnapshot(
            [new("one", "Rotas", "Rota", true, "ok", "manter"),
             new("two", "Schema", "Coluna", false, "ausente", "migrar")],
            DateTimeOffset.Parse("2026-09-03T00:00:00Z"), "pipeline");

        Assert.Equal(50, snapshot.Score);
        Assert.Equal(1, snapshot.Passed);
        Assert.Equal(1, snapshot.Failed);
        Assert.False(snapshot.IsApproved);
        Assert.Equal("migrar", snapshot.NextAction);
    }
}

public sealed class SchemaDriftGateServiceTests
{
    [Theory]
    [InlineData("documents", "assigned_team_id")]
    [InlineData("documents", "deleted_by")]
    [InlineData("budget_templates", "profession")]
    [InlineData("budget_templates", "created_at")]
    public void Critical_Contract_Contains_V62_Columns(string table, string column) =>
        Assert.True(DatabaseSchemaContractService.RegistrationContract[table].ContainsKey(column));

    [Fact]
    public void V62_Migration_Is_Required_Before_Readiness() =>
        Assert.Contains(DatabaseSchemaContractService.QualityGateSchemaDriftV62Migration,
            DatabaseSchemaContractService.RequiredMigrations);
}
