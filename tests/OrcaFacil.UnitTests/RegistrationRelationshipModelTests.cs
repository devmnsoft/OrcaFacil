using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class RegistrationRelationshipModelTests
{
    [Theory]
    [InlineData(typeof(AccountMember), nameof(AccountMember.AccountId), typeof(BusinessAccount))]
    [InlineData(typeof(AccountMember), nameof(AccountMember.UserId), typeof(UserAccount))]
    [InlineData(typeof(BillingCustomerProfile), nameof(BillingCustomerProfile.AccountId), typeof(BusinessAccount))]
    [InlineData(typeof(BillingCustomerProfile), nameof(BillingCustomerProfile.UserId), typeof(UserAccount))]
    [InlineData(typeof(Subscription), nameof(Subscription.AccountId), typeof(BusinessAccount))]
    [InlineData(typeof(Subscription), nameof(Subscription.UserId), typeof(UserAccount))]
    [InlineData(typeof(Subscription), nameof(Subscription.SelectedPlanVersionId), typeof(PlanVersion))]
    [InlineData(typeof(Subscription), nameof(Subscription.EffectivePlanVersionId), typeof(PlanVersion))]
    [InlineData(typeof(IssuerProfile), nameof(IssuerProfile.UserId), typeof(UserAccount))]
    [InlineData(typeof(Notification), nameof(Notification.AccountId), typeof(BusinessAccount))]
    [InlineData(typeof(Notification), nameof(Notification.UserId), typeof(UserAccount))]
    public void Registration_foreign_key_is_explicitly_mapped(Type dependent, string property, Type principal)
    {
        using var db = new OrcaFacilDbContext(new DbContextOptionsBuilder<OrcaFacilDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options);

        var entity = db.Model.FindEntityType(dependent);
        var foreignKey = Assert.Single(entity!.GetForeignKeys().Where(candidate =>
            candidate.Properties.Count == 1 && candidate.Properties[0].Name == property));

        Assert.Equal(principal, foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}
