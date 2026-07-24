using OrcaFacil.Persistence;
using Xunit;

namespace OrcaFacil.UnitTests;

public class DbContextContractTests
{
    [Theory]
    [InlineData("Users")]
    [InlineData("IssuerProfiles")]
    [InlineData("Documents")]
    [InlineData("DocumentItems")]
    [InlineData("PublicQuotes")]
    [InlineData("UserUsage")]
    [InlineData("Subscriptions")]
    [InlineData("Payments")]
    [InlineData("AdminSettings")]
    [InlineData("Notifications")]
    [InlineData("Clients")]
    [InlineData("BillingCustomerProfiles")]
    [InlineData("PlanFeatures")]
    [InlineData("PaymentEvents")]
    [InlineData("MercadoPagoWebhookEvents")]
    [InlineData("BudgetTemplates")]
    [InlineData("BudgetTemplateItems")]
    [InlineData("AuditLogs")]
    [InlineData("SystemLogs")]
    [InlineData("SystemErrors")]
    public void OrcaFacilDbContext_Exposes_Required_DbSets(string propertyName)
    {
        Assert.NotNull(typeof(OrcaFacilDbContext).GetProperty(propertyName));
    }
}
