using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class DocumentsSchemaDiagnosticsTests
{
    [Theory]
    [InlineData("client_snapshot")]
    [InlineData("conditions_text")]
    [InlineData("template_snapshot")]
    [InlineData("follow_up_status")]
    [InlineData("next_follow_up_at")]
    [InlineData("public_token")]
    [InlineData("client_decision")]
    [InlineData("internal_approval_status")]
    public void DocumentsContractRequiresCommercialColumn(string column) =>
        Assert.True(DatabaseSchemaContractService.RegistrationContract["documents"].ContainsKey(column));

    [Fact]
    public void CommercialRepairIsARequiredMigration() =>
        Assert.Contains(DatabaseSchemaContractService.CommercialDocumentRepairMigration, DatabaseSchemaContractService.RequiredMigrations);
}
