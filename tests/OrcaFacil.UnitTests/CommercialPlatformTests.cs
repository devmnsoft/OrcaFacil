using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialPlatformTests
{
    [Theory]
    [InlineData("FREE", 0, 0)]
    [InlineData("PROFESSIONAL", 24.90, 249)]
    [InlineData("BUSINESS", 49.90, 499)]
    public void Catalog_has_expected_prices(string code, decimal monthly, decimal annual)
    { var plan = PlanCatalogDefinitions.Plans[code]; Assert.Equal(monthly, plan.Monthly); Assert.Equal(annual, plan.Annual); }

    [Fact]
    public async Task Expired_paid_plan_falls_back_to_free_without_deleting_data()
    {
        var subscription = new Subscription { Plan = PlanType.Business, Status = SubscriptionStatus.Active, PaidThroughAt = DateTime.UtcNow.AddMinutes(-1) };
        var effective = await new PlanAccessService().GetEffectivePlanAsync(subscription, DateTime.UtcNow);
        Assert.Equal("FREE", effective);
        Assert.Equal(PlanType.Business, subscription.Plan);
    }

    [Fact]
    public async Task Late_payment_restores_selected_plan_when_paid_through_is_extended()
    {
        var now = DateTime.UtcNow;
        var subscription = new Subscription { Plan = PlanType.Business, Status = SubscriptionStatus.Active, PaidThroughAt = now.AddMonths(1), LastPaymentAt = now };
        Assert.Equal("BUSINESS", await new PlanAccessService().GetEffectivePlanAsync(subscription, now));
    }

    [Fact]
    public async Task Free_pdf_limit_is_enforced_in_backend()
    {
        var service = new PlanAccessService();
        Assert.True((await service.CanUseAsync("FREE", "pdf.monthly_limit", 9)).IsAllowed);
        Assert.False((await service.CanUseAsync("FREE", "pdf.monthly_limit", 10)).IsAllowed);
    }

    [Fact]
    public void Account_block_requires_reason()
    {
        var account = new BusinessAccount();
        Assert.Throws<ArgumentException>(() => account.Block(" "));
        account.Block("Análise de segurança");
        Assert.Equal(AccountStatus.Blocked, account.Status);
    }

    [Fact]
    public void Support_access_is_limited_to_thirty_minutes()
    {
        var start = DateTime.UtcNow;
        Assert.False(new SupportAccessSession { Reason = "Suporte solicitado", StartedAt = start, ExpiresAt = start.AddMinutes(31) }.IsValid(start.AddMinutes(1)));
        Assert.True(new SupportAccessSession { Reason = "Suporte solicitado", StartedAt = start, ExpiresAt = start.AddMinutes(30) }.IsValid(start.AddMinutes(1)));
    }
}
