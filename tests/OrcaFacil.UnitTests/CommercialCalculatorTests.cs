using OrcaFacil.Application.Commercial;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialCalculatorTests
{
    [Fact]
    public void CalculatesUsingSingleAwayFromZeroRoundingPolicy()
    {
        var result = CommercialCalculator.Calculate([new(2, 10.005m, 1, .50m)]);
        Assert.Equal(20.01m, result.Subtotal);
        Assert.Equal(19.51m, result.Total);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 10, 11)]
    public void RejectsInvalidCommercialValues(decimal quantity, decimal price, decimal discount) =>
        Assert.Throws<ArgumentException>(() => CommercialCalculator.Calculate([new(quantity, price, discount)]));

    [Fact]
    public void RejectsDocumentWithoutItems() =>
        Assert.Throws<ArgumentException>(() => CommercialCalculator.Calculate([]));
}
