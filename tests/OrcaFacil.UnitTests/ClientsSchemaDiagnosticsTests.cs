using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class ClientsSchemaDiagnosticsTests
{
    [Theory]
    [InlineData("account_id")]
    [InlineData("is_active")]
    [InlineData("is_deleted")]
    public void ClientsContractRequiresTenantAndLifecycleColumns(string column) =>
        Assert.True(DatabaseSchemaContractService.RegistrationContract["clients"].ContainsKey(column));

    [Fact]
    public void DriftRepairIsRequired() =>
        Assert.Contains(DatabaseSchemaContractService.ClientsAndDocumentsDriftMigration,
            DatabaseSchemaContractService.RequiredMigrations);
}
