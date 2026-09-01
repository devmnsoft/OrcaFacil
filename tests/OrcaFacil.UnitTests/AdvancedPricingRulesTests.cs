using OrcaFacil.Application.Pricing;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class AdvancedPricingRulesTests
{
    [Fact]
    public void Contract_table_has_priority_over_customer_and_account()
    {
        var account = Guid.NewGuid(); var customer = Guid.NewGuid(); var contract = Guid.NewGuid();
        var tables = new[] {
            Table(account, PriceTableScope.Account),
            Table(account, PriceTableScope.Customer, customer: customer),
            Table(account, PriceTableScope.Contract, contract: contract) };
        Assert.Equal(PriceTableScope.Contract, AdvancedPricingRules.SelectTable(tables, account, DateOnly.FromDateTime(DateTime.UtcNow), customer, null, contract).Scope);
    }

    [Fact]
    public void Expired_table_is_not_applied()
    {
        var account = Guid.NewGuid(); var table = Table(account, PriceTableScope.Account);
        table.ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        Assert.Throws<InvalidOperationException>(() => AdvancedPricingRules.SelectTable([table], account, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void Snapshot_is_immutable_copy_of_calculation()
    {
        var snapshot = AdvancedPricingRules.Snapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 100, 10, 60, Guid.NewGuid(), "{\"serviceVersion\":2}");
        Assert.Equal(90, snapshot.TotalPrice); Assert.Equal(33.33m, snapshot.MarginPercentage);
    }

    [Fact]
    public void Discount_above_total_is_rejected() => Assert.Throws<ArgumentException>(() =>
        AdvancedPricingRules.Snapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 10, 11, 1, Guid.NewGuid(), "{}"));

    [Fact]
    public void Approver_cannot_decide_without_justification()
    {
        var approver = Guid.NewGuid();
        var approval = new PricingApprovalEvent { RequestedByUserId = Guid.NewGuid(), ApproverUserId = approver };
        Assert.Throws<ArgumentException>(() => AdvancedPricingRules.Decide(approval, approver, true, " "));
    }

    private static ServicePriceTable Table(Guid account, PriceTableScope scope, Guid? customer = null, Guid? contract = null) => new()
    { AccountId = account, Name = scope.ToString(), Scope = scope, CustomerId = customer, ContractId = contract,
      ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };
}
