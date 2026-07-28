using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.UnitTests;

public sealed class BillingProfileSchemaRepairTests
{
    [Fact]
    public void RegistrationContract_RequiresMercadoPagoCustomerId()
    {
        var columns = DatabaseSchemaContractService.RegistrationContract["billing_customer_profiles"];

        Assert.Equal("character varying", columns["mercado_pago_customer_id"]);
    }

    [Fact]
    public void RegistrationContract_AuditsTheCompleteBillingProfile()
    {
        var expected = new[]
        {
            "id", "account_id", "user_id", "person_type", "document_type", "document_number", "name",
            "trade_name", "legal_name", "email", "phone", "city", "state", "postal_code", "street",
            "street_number", "complement", "district", "address", "mercado_pago_customer_id", "created_at",
            "updated_at", "is_deleted"
        };

        Assert.Equal(expected, DatabaseSchemaContractService.RegistrationContract["billing_customer_profiles"].Keys);
    }

    [Fact]
    public void RepairMigration_UsesAnIdempotentNullableColumnStatement()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Migrations",
            "20260728210000_RepairBillingCustomerProfileSchema.cs"));

        Assert.Contains("ADD COLUMN IF NOT EXISTS mercado_pago_customer_id varchar(180)", migration);
        Assert.DoesNotContain("mercado_pago_customer_id varchar(180) NOT NULL", migration);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
