using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class DocumentsRowVersionSchemaTests
{
    [Fact]
    public void Diagnostics_Requires_Bytea_RowVersion() =>
        Assert.Equal("bytea", DatabaseDiagnosticsService.RequiredDocumentColumns["row_version"]);

    [Fact]
    public void Document_Can_Advance_Its_Application_Managed_Token()
    {
        var document = new Document { UserId = Guid.NewGuid(), ClientName = "Cliente" };
        var original = document.RowVersion.ToArray();

        document.AdvanceRowVersion();

        Assert.NotEqual(original, document.RowVersion);
        Assert.Equal(16, document.RowVersion.Length);
    }
}

public sealed class DocumentsCommercialSchemaTests
{
    [Theory]
    [InlineData("payment_method", "character varying")]
    [InlineData("deposit_amount", "numeric")]
    [InlineData("client_snapshot", "jsonb")]
    [InlineData("template_snapshot", "jsonb")]
    public void Diagnostics_Requires_Commercial_Column_With_Ef_Type(string column, string type) =>
        Assert.Equal(type, DatabaseDiagnosticsService.RequiredDocumentColumns[column]);
}

public sealed class SchemaDriftDiagnosticsTests
{
    [Fact]
    public void Critical_Document_Contract_Is_Complete()
    {
        var expected = new[] { "row_version", "conditions_text", "public_token", "next_follow_up_at", "origin_budget_id" };
        Assert.All(expected, column => Assert.Contains(column, DatabaseDiagnosticsService.RequiredDocumentColumns.Keys));
    }
}

public sealed class SystemHealthSchemaTests
{
    [Fact]
    public void Drift_Issue_Contains_Actionable_Context()
    {
        var issue = new OrcaFacil.Application.Abstractions.SchemaDriftIssue(
            "documents.row_version", "MissingColumn", "Critical", "Commercial",
            ["/Dashboard"], "database/hotfix_documents_row_version_schema_drift_v60.sql", "bytea");
        Assert.Contains("/Dashboard", issue.ImpactedRoutes);
        Assert.Equal("bytea", issue.ExpectedType);
    }
}
