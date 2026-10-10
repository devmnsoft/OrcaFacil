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

    [Fact]
    public void Blocking_warnings_mark_the_balance_as_not_safe_for_new_operations()
    {
        var result = CommercialBalanceCalculator.Calculate(Guid.NewGuid(), Guid.NewGuid(), "WorkOrder", 1000m,
            [new CommercialPaymentAmount(250m, IsReversed: false)],
            blockingWarnings: ["Vínculo financeiro incompatível."]);

        Assert.True(result.HasBlockingDivergence);
        Assert.Equal(750m, result.BalanceAmount);
    }

    [Fact]
    public void Blocking_divergence_prevents_settled_status_even_when_operational_balance_is_zero()
    {
        var result = CommercialBalanceCalculator.Calculate(Guid.NewGuid(), Guid.NewGuid(), "WorkOrder", 1000m,
            [new CommercialPaymentAmount(1000m, IsReversed: false)],
            blockingWarnings: ["Vínculo financeiro incompatível."]);

        Assert.Equal(0m, result.BalanceAmount);
        Assert.True(result.HasBlockingDivergence);
        Assert.False(result.IsSettled);
    }
}
