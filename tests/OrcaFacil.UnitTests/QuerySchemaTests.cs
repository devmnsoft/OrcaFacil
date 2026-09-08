using Xunit;

namespace OrcaFacil.UnitTests;

public class QuerySchemaTests
{
    [Theory]
    [InlineData("src/OrcaFacil.Persistence/Queries/DocumentQueries.cs", "orcafacil.documents")]
    [InlineData("src/OrcaFacil.Persistence/Queries/DashboardQueries.cs", "orcafacil.documents")]
    [InlineData("src/OrcaFacil.Persistence/Queries/DashboardQueries.cs", "orcafacil.user_usage")]
    [InlineData("src/OrcaFacil.Persistence/Queries/DashboardQueries.cs", "orcafacil.users")]
    public void DapperQueries_Use_OrcaFacil_Schema(string relativePath, string expectedSchemaTable)
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, relativePath));

        Assert.Contains(expectedSchemaTable, source);
    }

    [Theory]
    [InlineData("identity.")]
    [InlineData("core.")]
    [InlineData("billing.")]
    [InlineData("admin.")]
    [InlineData("logs.")]
    [InlineData("public_access.")]
    public void DatabaseScript_Does_Not_Use_Legacy_Schemas(string legacySchema)
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "database/script_completop.sql"));

        // Permission codes such as Admin.Access and ApiLogs.View are string data,
        // not PostgreSQL schema qualifiers. Remove SQL literals before inspecting identifiers.
        var identifiersOnly = System.Text.RegularExpressions.Regex.Replace(script, @"'(?:''|[^'])*'", "''",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.DoesNotMatch($@"(?i)\b{System.Text.RegularExpressions.Regex.Escape(legacySchema.TrimEnd('.'))}\s*\.\s*[\""']?[a-z_]", identifiersOnly);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
