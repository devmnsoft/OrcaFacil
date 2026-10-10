using OrcaFacil.Application.Commercial;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialBalanceCalculatorTests
{
    [Fact]
    public void Zero_contract_is_a_valid_settled_balance()
    {
        var result = CommercialBalanceCalculator.Calculate(Guid.NewGuid(), null, "CurrentRevision", 0m, []);

        Assert.Equal(0m, result.ContractedAmount);
        Assert.Equal(0m, result.BalanceAmount);
        Assert.Equal(0m, result.OverpaidAmount);
        Assert.True(result.IsSettled);
    }

    [Fact]
    public void Reversed_payments_are_reported_without_reducing_active_received_twice()
    {
        var result = CommercialBalanceCalculator.Calculate(Guid.NewGuid(), Guid.NewGuid(), "WorkOrder", 1000m,
        [
            new CommercialPaymentAmount(300m, IsReversed: false),
            new CommercialPaymentAmount(200m, IsReversed: true)
        ]);

        Assert.Equal(1000m, result.ContractedAmount);
        Assert.Equal(300m, result.ReceivedAmount);
        Assert.Equal(200m, result.ReversedAmount);
        Assert.Equal(700m, result.BalanceAmount);
        Assert.Equal(0m, result.OverpaidAmount);
    }

    [Fact]
    public void Overpayment_is_exposed_instead_of_hidden_in_zero_balance()
    {
        var result = CommercialBalanceCalculator.Calculate(Guid.NewGuid(), Guid.NewGuid(), "WorkOrder", 1000m,
        [
            new CommercialPaymentAmount(1200m, IsReversed: false)
        ]);

        Assert.Equal(1200m, result.ReceivedAmount);
        Assert.Equal(0m, result.BalanceAmount);
        Assert.Equal(200m, result.OverpaidAmount);
        Assert.True(result.HasOverpayment);
        Assert.False(result.IsSettled);
    }
}
