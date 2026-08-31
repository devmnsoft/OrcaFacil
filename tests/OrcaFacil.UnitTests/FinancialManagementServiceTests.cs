using OrcaFacil.Application.Finance;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class FinancialCashFlowServiceTests
{
    [Fact] public void Uses_only_real_tenant_movements_and_does_not_count_transfers()
    {
        var tenant = Guid.NewGuid(); var day = new DateOnly(2026, 8, 31); var service = new FinancialManagementService();
        var result = service.CalculateCashFlow(tenant, 100m, day, day,
        [
            Entry(tenant, ManagementEntryKind.Revenue, 80m, day), Entry(tenant, ManagementEntryKind.Expense, 30m, day),
            Entry(tenant, ManagementEntryKind.Transfer, 999m, day), Entry(Guid.NewGuid(), ManagementEntryKind.Revenue, 500m, day)
        ]);
        Assert.Equal(new CashFlowSummary(100m, 80m, 30m, 150m), result);
    }

    internal static ManagementEntry Entry(Guid account, ManagementEntryKind kind, decimal amount, DateOnly day, Guid? chart = null, DateOnly? realized = null, string? reason = "origem manual") =>
        new(account, Guid.NewGuid(), kind, amount, day, realized ?? day, day, chart, null, false, reason);
}

public sealed class FinancialCashFlowProjectionServiceTests
{
    [Fact] public void Separates_confirmed_expected_and_scenario_using_declared_risk()
    {
        var account = Guid.NewGuid(); var day = new DateOnly(2026, 9, 15); var service = new FinancialManagementService();
        var result = service.Project(account, day, day,
        [new(account, Guid.NewGuid(), 100m, day, true, ProjectionConfidence.Confirmed, "recebível aberto"), new(account, Guid.NewGuid(), 40m, day, false, ProjectionConfidence.Expected, "pagável aberto", .5m), new(account, Guid.NewGuid(), 10m, day, true, ProjectionConfidence.Scenario, "cenário informado")]);
        Assert.Equal((day, 100m, -20m, 10m), result.Single());
    }
    [Fact] public void Requires_projection_basis() { var account = Guid.NewGuid(); Assert.Throws<InvalidOperationException>(() => new FinancialManagementService().Project(account, new(2026, 1, 1), new(2026, 1, 1), [new(account, Guid.NewGuid(), 1, new(2026, 1, 1), true, ProjectionConfidence.Expected, "")])); }
}

public sealed class FinancialDreServiceTests
{
    [Fact] public void Respects_cash_and_accrual_dates_and_tenant()
    {
        var tenant = Guid.NewGuid(); var revenue = Guid.NewGuid(); var day = new DateOnly(2026, 8, 1); var realized = day.AddMonths(1); var service = new FinancialManagementService();
        var facts = new[] { FinancialCashFlowServiceTests.Entry(tenant, ManagementEntryKind.Revenue, 200m, day, revenue, realized) };
        Assert.Equal(200m, service.CalculateDre(tenant, day, day, FinancialRegime.Accrual, facts, new HashSet<Guid>{revenue}, [], [], []).GrossRevenue);
        Assert.Equal(0m, service.CalculateDre(tenant, day, day, FinancialRegime.Cash, facts, new HashSet<Guid>{revenue}, [], [], []).GrossRevenue);
    }
}

public sealed class FinancialEntryServiceTests
{
    [Fact] public void Manual_entry_requires_reason_and_positive_value()
    {
        var day = new DateOnly(2026, 8, 1); var invalid = new ManagementEntry(Guid.NewGuid(), Guid.NewGuid(), ManagementEntryKind.Expense, 10m, day, null, day);
        Assert.Throws<InvalidOperationException>(() => FinancialManagementService.ValidateManualEntry(invalid, false));
        Assert.Throws<InvalidOperationException>(() => FinancialManagementService.ValidateManualEntry(invalid with { Amount = -1, ManualReason = "correção" }, false));
    }
}

public sealed class FinancialAllocationServiceTests
{
    [Fact] public void Percentage_must_close_at_100_and_value_must_reconcile()
    {
        FinancialManagementService.ValidateAllocation(100m, [new(Guid.NewGuid(), 60m, 60m), new(Guid.NewGuid(), 40m, 40m)]);
        Assert.Throws<InvalidOperationException>(() => FinancialManagementService.ValidateAllocation(100m, [new(Guid.NewGuid(), 99m, 100m)]));
    }
    [Fact] public void Duplicate_cost_center_is_rejected() { var center = Guid.NewGuid(); Assert.Throws<InvalidOperationException>(() => FinancialManagementService.ValidateAllocation(10m, [new(center, 50m, 5m), new(center, 50m, 5m)])); }
}

public sealed class FinancialMonthlyClosingServiceTests
{
    [Fact] public void Closing_requires_complete_checklist() => Assert.Throws<InvalidOperationException>(() => FinancialManagementService.ValidateClosing(new(true,true,true,true,true,true,true,true,true,false)));
    [Fact] public void Reopening_requires_permission_and_reason() { Assert.Throws<UnauthorizedAccessException>(() => FinancialManagementService.ValidateReopening("correção", false)); Assert.Throws<InvalidOperationException>(() => FinancialManagementService.ValidateReopening("", true)); }
}

public sealed class FinanceTenantIsolationTests
{
    [Fact] public void Other_account_never_contributes_to_management_result()
    {
        var tenant = Guid.NewGuid(); var account = Guid.NewGuid(); var day = new DateOnly(2026, 8, 1);
        var result = new FinancialManagementService().CalculateDre(tenant, day, day, FinancialRegime.Accrual, [FinancialCashFlowServiceTests.Entry(Guid.NewGuid(), ManagementEntryKind.Revenue, 999m, day, account)], new HashSet<Guid>{account}, [], [], []);
        Assert.Equal(0m, result.ManagementResult);
    }
}
