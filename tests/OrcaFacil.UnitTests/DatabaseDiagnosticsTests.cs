using Microsoft.Extensions.Configuration;
using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public class DatabaseDiagnosticsTests
{
    [Fact]
    public void RequiredTables_Contains_All_Mvp_Tables()
    {
        var expected = new[]
        {
                    "users", "issuer_profiles", "documents", "document_items", "public_quotes",
            "user_usage", "subscriptions", "payments", "payment_events", "mercadopago_webhook_events",
            "billing_customer_profiles", "clients", "plan_features", "admin_settings", "notifications",
            "audit_logs", "system_logs", "system_errors", "business_accounts", "account_members",
            "plans", "plan_versions"
        };

        Assert.Equal(expected, DatabaseDiagnosticsService.RequiredTables);
    }

    [Fact]
    public void MissingTable_Detection_Can_Be_Modeled_Without_Postgres()
    {
        var existing = new HashSet<string>(["users", "documents"], StringComparer.OrdinalIgnoreCase);
        var missing = DatabaseDiagnosticsService.RequiredTables.Where(table => !existing.Contains(table)).ToArray();

        Assert.Contains("issuer_profiles", missing);
        Assert.Contains("system_errors", missing);
        Assert.DoesNotContain("users", missing);
    }

    [Fact]
    public void MaskConnectionString_Hides_Password()
    {
        var masked = DatabaseDiagnosticsService.MaskConnectionString("Host=localhost;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=<redacted>");

        Assert.Contains("Password=******", masked);
        Assert.DoesNotContain("123456", masked);
    }

    [Fact]
    public async Task CheckAsync_Returns_Error_When_ConnectionString_Is_Missing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        var service = new DatabaseDiagnosticsService(configuration);

        var result = await service.CheckAsync();

        Assert.False(result.CanConnect);
        Assert.Equal(DatabaseDiagnosticsService.RequiredTables, result.MissingTables);
        Assert.Contains("DefaultConnection", result.Error);
    }
}
