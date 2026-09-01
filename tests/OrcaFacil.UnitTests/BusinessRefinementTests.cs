using OrcaFacil.Application.Quality;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class BusinessRuleTransitionTests
{
    private readonly BusinessTransitionRuleService _service = new(new BusinessStatusCatalogService());

    [Fact]
    public void Invalid_transition_is_blocked() =>
        Assert.Throws<InvalidOperationException>(() => _service.EnsureAllowed(BusinessEntityKind.Proposal, "Draft", "Approved", true));

    [Fact]
    public void Critical_transition_requires_permission() =>
        Assert.Throws<UnauthorizedAccessException>(() => _service.EnsureAllowed(BusinessEntityKind.Proposal, "Sent", "Approved", false));

    [Fact]
    public void Done_task_requires_reason_to_reopen() =>
        Assert.Throws<ArgumentException>(() => _service.EnsureAllowed(BusinessEntityKind.Task, "Done", "InProgress", true));
}

public sealed class MoneyCalculationTests
{
    [Theory]
    [InlineData(100, 10, 90)]
    [InlineData(10.01, 33.33, 6.67)]
    public void Percentage_discount_is_decimal_and_explicitly_rounded(decimal subtotal, decimal percentage, decimal expected) =>
        Assert.Equal(expected, MoneyCalculator.ApplyPercentageDiscount(subtotal, percentage));

    [Fact]
    public void Fixed_discount_cannot_make_total_negative() =>
        Assert.Throws<ArgumentException>(() => MoneyCalculator.ApplyFixedDiscount(10m, 10.01m));

    [Fact]
    public void Retention_cannot_make_net_negative() =>
        Assert.Throws<ArgumentException>(() => MoneyCalculator.NetAfterRetention(10m, 11m));
}

public sealed class DateTimePolicyTests
{
    [Fact]
    public void Due_date_cannot_precede_issue_date() =>
        Assert.Throws<ArgumentException>(() => new DueDatePolicyService().Validate(new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 30)));
}

 

public sealed class FinancialFlowConsistencyTests
{
    [Fact]
    public void Pending_payment_cannot_generate_receipt() =>
        Assert.Throws<InvalidOperationException>(() => FinancialFlowPolicy.EnsureReceiptAllowed(PaymentStatus.Pending));

    [Fact]
    public void Confirmed_payment_can_generate_receipt() => FinancialFlowPolicy.EnsureReceiptAllowed(PaymentStatus.Approved);
}

public sealed class ModuleRefinementScoreServiceTests
{
    [Fact]
    public void Score_is_derived_from_real_checks()
    {
        var result = new ModuleRefinementScoreService().Calculate([new("tenant", true), new("permission", false), new("flow", true)]);
        Assert.Equal(67, result.Score);
        Assert.Equal(2, result.Passed);
        Assert.Equal(3, result.Total);
    }
}
