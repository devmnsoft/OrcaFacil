using OrcaFacil.Application.Growth;

namespace OrcaFacil.UnitTests;

public sealed class GrowthRulesTests
{
    [Fact]
    public void Attribution_without_marketing_parameters_is_direct_unknown()
    {
        var result = new GrowthAttributionService().Normalize(new(null, null, null, null, null, null, null, null, "/Demo", null));
        Assert.Equal("Direct/Unknown", result.Source);
        Assert.Equal("Direct/Unknown", result.Channel);
    }

    [Fact]
    public void Score_is_deterministic_and_explains_each_signal()
    {
        var signals = new GrowthLeadSignals("Serviços", "11-50", "PRO", "Google", true, false, true, true, true, true, 2);
        var service = new GrowthLeadScoreService();
        var first = service.Calculate(signals);
        var result = service.Calculate(signals);
        Assert.Equal(first.Score, result.Score);
        Assert.Equal(first.Classification, result.Classification);
        Assert.Equal("Quente", result.Classification);
        Assert.NotEmpty(result.Reasons);
        Assert.NotEmpty(result.NextAction);
    }

    [Fact]
    public void Expired_coupon_does_not_apply()
    {
        var now = DateTimeOffset.UtcNow;
        var coupon = new CouponRule(true, now.AddDays(-10), now.AddDays(-1), GrowthDiscountType.Percentage, 20, null, 0, 1, 0);
        Assert.False(new CouponService().Apply(coupon, 100, now).Applied);
    }

    [Theory]
    [InlineData(150, 50, 0)]
    [InlineData(25, 100, 0)]
    public void Coupon_never_makes_total_negative(decimal discount, decimal subtotal, decimal expected)
    {
        var now = DateTimeOffset.UtcNow;
        var coupon = new CouponRule(true, now.AddDays(-1), now.AddDays(1), GrowthDiscountType.FixedAmount, discount, null, 0, 1, 0);
        Assert.Equal(expected, new CouponService().Apply(coupon, subtotal, now).Total);
    }

    [Fact]
    public void Commission_requires_confirmed_real_payment()
    {
        var service = new ResellerCommissionService();
        Assert.Equal(0, service.Calculate(1000, 10, false, true));
        Assert.Equal(100, service.Calculate(1000, 10, true, true));
    }

    [Fact]
    public void Mrr_excludes_trials_and_reversed_payments()
    {
        var metrics = new[] { new PaidSubscriptionMetric(100, false, true, false), new(200, true, true, false), new(300, false, true, true) };
        Assert.Equal(100, new GrowthRevenueService().CalculateMrr(metrics));
    }
}
