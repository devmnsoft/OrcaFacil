using OrcaFacil.Application.Bi;

namespace OrcaFacil.UnitTests;

public sealed class BiExecutiveServicesTests
{
    private static readonly BiPeriod Period = new(new(2026, 8, 1), new(2026, 8, 31));

    [Fact] public void Sensitive_metric_requires_explicit_permission()
    {
        var metric = new BiMetricDefinition("margin", "Margem", "Financeiro", "receita-custo", "DRE", true);
        Assert.False(new BiMetricPermissionService().CanView(metric, new HashSet<string> { BiPermissions.MetricsView }));
    }

    [Fact] public void Trend_handles_zero_without_division()
    { var result = new BiTrendAnalysisService().Compare(10, 0); Assert.False(result.HasComparableBase); Assert.Null(result.PercentageChange); }

    [Fact] public void Goal_progress_uses_linked_real_metric()
    {
        var metric = new BiMetricDefinition("revenue", "Receita", "Financeiro", "sum(payments)", "Pagamentos", false);
        var goal = new BiGoalService().Create(Guid.NewGuid(), "Receita mensal", metric, 200, Period);
        var actual = new BiMetricValue("revenue", 50, Period, "Pagamentos", DateTimeOffset.UtcNow);
        Assert.Equal(25, new BiGoalService().Progress(goal, actual));
    }

    [Fact] public void Okr_cannot_activate_without_key_result()
    { Assert.Throws<InvalidOperationException>(() => new OkrService().Activate(new(Guid.NewGuid(), Guid.NewGuid(), "Crescer", Period, [], BiProgressStatus.Draft))); }

    [Fact] public void Alert_deduplicates_open_rule_by_account_and_period()
    {
        var account = Guid.NewGuid(); var alerts = new List<BiAlert>(); var service = new BiAlertService();
        service.Add(alerts, account, "late-os", "High", "Existem OS atrasadas", "/WorkOrders", Period);
        Assert.Throws<InvalidOperationException>(() => service.Add(alerts, account, "late-os", "High", "Existem OS atrasadas", "/WorkOrders", Period));
    }

    [Fact] public void Data_mart_refresh_is_idempotent()
    {
        var items = new List<BiDataMartSnapshot>(); var service = new BiDataMartService(); var account = Guid.NewGuid();
        var first = service.Refresh(items, account, Period, "payments-v1"); var second = service.Refresh(items, account, Period, "payments-v1");
        Assert.Same(first, second); Assert.Single(items);
    }

    [Fact] public void Forecast_reports_insufficient_history_and_preserves_assumptions()
    { var result = new BiForecastService().Linear([10, 20], "Base", ["Média da variação mensal"]); Assert.False(result.HasSufficientData); Assert.Null(result.Value); }

    [Fact] public void Dashboard_is_tenant_isolated()
    {
        var account = Guid.NewGuid(); var other = Guid.NewGuid();
        var data = new[] { new BiDashboard(Guid.NewGuid(), account, "A", Period, []), new BiDashboard(Guid.NewGuid(), other, "B", Period, []) };
        Assert.All(new BiDashboardService().ForAccount(data, account), x => Assert.Equal(account, x.AccountId));
    }

    [Fact] public void Insight_requires_real_internal_source()
    { Assert.Throws<ArgumentException>(() => new BiInsightService().Create(Guid.NewGuid(), "Risco", "#", "Revisar")); }
}
