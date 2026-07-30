using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialJourneySchemaContractTests
{
    [Fact]
    public void RegistrationContract_IncludesEveryCriticalCommercialAndDeliveryTable()
    {
        string[] criticalTables =
        [
            "document_revisions", "public_document_accesses", "public_document_decisions",
            "commercial_follow_ups", "work_orders", "password_reset_tokens", "email_outbox_messages"
        ];

        Assert.All(criticalTables, table =>
            Assert.True(DatabaseSchemaContractService.RegistrationContract.ContainsKey(table), $"Missing contract for {table}."));
    }

    [Fact]
    public void RequiredMigrations_IncludesRecoveryOutboxAndCommercialJourney()
    {
        Assert.Contains(DatabaseSchemaContractService.PasswordRecoveryMigration, DatabaseSchemaContractService.RequiredMigrations);
        Assert.Contains(DatabaseSchemaContractService.CommercialJourneyMigration, DatabaseSchemaContractService.RequiredMigrations);
    }

    [Theory]
    [InlineData("document_revisions", "snapshot_hash")]
    [InlineData("public_document_accesses", "token_hash")]
    [InlineData("public_document_decisions", "idempotency_key")]
    [InlineData("work_orders", "source_revision_id")]
    [InlineData("email_outbox_messages", "protected_recipient")]
    public void RegistrationContract_IncludesSecurityAndConcurrencyColumns(string table, string column)
    {
        Assert.True(DatabaseSchemaContractService.RegistrationContract[table].ContainsKey(column));
    }
}
