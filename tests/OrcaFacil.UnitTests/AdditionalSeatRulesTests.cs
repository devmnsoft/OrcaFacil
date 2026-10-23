using OrcaFacil.Application.Plans;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class AdditionalSeatRulesTests
{
    [Fact]
    public void AddLimitIncrement_keeps_unlimited_as_null()
    {
        Assert.Null(AdditionalSeatRules.AddLimitIncrement(null, 5));
    }

    [Fact]
    public void AddLimitIncrement_adds_purchased_seats_and_clamps_negative()
    {
        Assert.Equal(5, AdditionalSeatRules.AddLimitIncrement(3, 2));
        Assert.Equal(3, AdditionalSeatRules.AddLimitIncrement(3, -1));
        Assert.Equal(3, AdditionalSeatRules.AddLimitIncrement(3, 0));
    }

    [Theory]
    [InlineData("BUSINESS", true)]
    [InlineData("business", true)]
    [InlineData("FREE", false)]
    [InlineData("PROFESSIONAL", false)]
    [InlineData("ENTERPRISE", false)]
    [InlineData(null, false)]
    public void IsEligiblePlan_only_business_can_buy_seats(string? planCode, bool expected)
    {
        Assert.Equal(expected, AdditionalSeatRules.IsEligiblePlan(planCode));
    }

    [Fact]
    public void ValidatePurchase_accepts_valid_quantities()
    {
        Assert.Null(AdditionalSeatRules.ValidatePurchase(0, 2));
        Assert.Null(AdditionalSeatRules.ValidatePurchase(3, 2)); // sobra vaga para mais 2
        Assert.Null(AdditionalSeatRules.ValidatePurchase(4, 1));
    }

    [Fact]
    public void ValidatePurchase_blocks_beyond_max_additional_seats()
    {
        Assert.NotNull(AdditionalSeatRules.ValidatePurchase(0, 0));    // quantidade mínima
        Assert.NotNull(AdditionalSeatRules.ValidatePurchase(0, 6));    // acima do teto por vez
        Assert.NotNull(AdditionalSeatRules.ValidatePurchase(4, 2));    // estoura o que sobra
        Assert.Contains("máximo", AdditionalSeatRules.ValidatePurchase(5, 1)!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MonthlyCost_multiplies_seat_price()
    {
        Assert.Equal(39.80m, AdditionalSeatRules.MonthlyCost(2));
        Assert.Equal(0m, AdditionalSeatRules.MonthlyCost(0));
        Assert.Equal(0m, AdditionalSeatRules.MonthlyCost(-3));
    }
}
