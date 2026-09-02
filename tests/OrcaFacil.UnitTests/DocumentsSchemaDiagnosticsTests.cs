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
    [InlineData("deposit_amount")]
    [InlineData("installment_count")]
    [InlineData("estimated_duration")]
    [InlineData("expected_start_at")]
    [InlineData("requires_internal_approval")]
    public void DocumentsContractRequiresCommercialColumn(string column) =>
        Assert.True(DatabaseSchemaContractService.RegistrationContract["documents"].ContainsKey(column));

    [Fact]
    public void CommercialRepairIsARequiredMigration() =>
        Assert.Contains(DatabaseSchemaContractService.CommercialDocumentRepairMigration, DatabaseSchemaContractService.RequiredMigrations);

    [Fact]
    public void DepositAmountRepairIsARequiredMigration() =>
        Assert.Contains(DatabaseSchemaContractService.DepositAmountDriftMigration, DatabaseSchemaContractService.RequiredMigrations);
}
