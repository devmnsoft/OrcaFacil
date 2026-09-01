namespace OrcaFacil.Application.Bi;

public static class BiPermissions
{
    public const string View = "BI.View";
    public const string Manage = "BI.Manage";
    public const string CockpitView = "BI.CockpitView";
    public const string DashboardsView = "BI.DashboardsView";
    public const string DashboardsManage = "BI.DashboardsManage";
    public const string MetricsView = "BI.MetricsView";
    public const string MetricsManage = "BI.MetricsManage";
    public const string GoalsView = "BI.GoalsView";
    public const string GoalsManage = "BI.GoalsManage";
    public const string OkrsView = "BI.OkrsView";
    public const string OkrsManage = "BI.OkrsManage";
    public const string AlertsView = "BI.AlertsView";
    public const string AlertsManage = "BI.AlertsManage";
    public const string ReportsView = "BI.ReportsView";
    public const string ReportsExport = "BI.ReportsExport";
    public const string DataMartView = "BI.DataMartView";
    public const string DataMartManage = "BI.DataMartManage";
    public const string ForecastView = "BI.ForecastView";
    public const string InsightsView = "BI.InsightsView";
    public const string SensitiveFinancialMetricsView = "BI.SensitiveFinancialMetricsView";
    public const string GlobalView = "BI.GlobalView";
}

public sealed record BiPeriod(DateOnly Start, DateOnly End)
{
    public int Days => End.DayNumber - Start.DayNumber + 1;
    public void Validate() { if (End < Start) throw new ArgumentException("O fim do período deve ser igual ou posterior ao início."); }
}
public sealed record BiMetricDefinition(string Code, string Name, string Category, string Formula, string Source, bool Sensitive, bool Active = true);
public sealed record BiMetricValue(string Code, decimal Value, BiPeriod Period, string Source, DateTimeOffset CalculatedAt);

public sealed class BiMetricPermissionService
{
    public bool CanView(BiMetricDefinition metric, IReadOnlySet<string> permissions) =>
        permissions.Contains(BiPermissions.MetricsView) && (!metric.Sensitive || permissions.Contains(BiPermissions.SensitiveFinancialMetricsView));
    public void DemandView(BiMetricDefinition metric, IReadOnlySet<string> permissions)
    { if (!CanView(metric, permissions)) throw new UnauthorizedAccessException("Indicador não permitido para este perfil."); }
}

public interface IBiMetricDataSource
{
    Task<decimal?> CalculateAsync(Guid accountId, string metricCode, BiPeriod period, CancellationToken cancellationToken);
}

public sealed class BiMetricCalculationService(IBiMetricDataSource source, BiMetricPermissionService permissions)
{
    public async Task<BiMetricValue?> CalculateAsync(Guid accountId, BiMetricDefinition metric, BiPeriod period,
        IReadOnlySet<string> grantedPermissions, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("A conta é obrigatória.", nameof(accountId));
        period.Validate();
        permissions.DemandView(metric, grantedPermissions);
        if (!metric.Active) throw new InvalidOperationException("O indicador está inativo.");
        var value = await source.CalculateAsync(accountId, metric.Code, period, cancellationToken);
        return value is null ? null : new(metric.Code, value.Value, period, metric.Source, DateTimeOffset.UtcNow);
    }
}

public sealed record BiComparison(decimal Current, decimal Previous, decimal? PercentageChange, bool HasComparableBase);
public sealed class BiTrendAnalysisService
{
    public BiComparison Compare(decimal current, decimal previous) => previous == 0
        ? new(current, previous, null, false)
        : new(current, previous, decimal.Round((current - previous) / Math.Abs(previous) * 100, 2), true);
}

public enum BiProgressStatus { Draft, Active, OnTrack, AtRisk, OffTrack, Achieved, Completed, Canceled, Closed }
public sealed record BiGoal(Guid Id, Guid AccountId, string Name, string MetricCode, decimal Target, BiPeriod Period, BiProgressStatus Status);
public sealed class BiGoalService
{
    public BiGoal Create(Guid accountId, string name, BiMetricDefinition metric, decimal target, BiPeriod period)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("A conta é obrigatória.");
        period.Validate();
        if (!metric.Active) throw new ArgumentException("A meta exige uma métrica ativa.");
        if (string.IsNullOrWhiteSpace(name) || target <= 0) throw new ArgumentException("Nome e alvo positivo são obrigatórios.");
        return new(Guid.NewGuid(), accountId, name.Trim(), metric.Code, target, period, BiProgressStatus.Draft);
    }
    public decimal Progress(BiGoal goal, BiMetricValue actual)
    {
        if (goal.MetricCode != actual.Code) throw new InvalidOperationException("O realizado deve vir da métrica vinculada.");
        return decimal.Clamp(decimal.Round(actual.Value / goal.Target * 100, 2), 0, 100);
    }
}

public sealed record BiKeyResult(Guid Id, string Name, string? MetricCode, decimal Target, decimal Current);
public sealed record BiObjective(Guid Id, Guid AccountId, string Name, BiPeriod Cycle, IReadOnlyList<BiKeyResult> KeyResults, BiProgressStatus Status);
public sealed class OkrService
{
    public BiObjective Activate(BiObjective objective)
    {
        objective.Cycle.Validate();
        if (objective.KeyResults.Count == 0) throw new InvalidOperationException("O objetivo exige ao menos um resultado-chave.");
        return objective with { Status = BiProgressStatus.Active };
    }
    public BiKeyResult RecordManualProgress(BiKeyResult result, decimal value, string auditReason)
    {
        if (result.MetricCode is not null) throw new InvalidOperationException("Resultado vinculado a KPI é atualizado pelo motor de métricas.");
        if (string.IsNullOrWhiteSpace(auditReason)) throw new ArgumentException("A atualização manual exige justificativa auditável.");
        return result with { Current = value };
    }
}

public sealed record BiAlert(Guid Id, Guid AccountId, string RuleCode, string Severity, string Reason, string SourceUrl, BiPeriod Period, bool Acknowledged);
public sealed class BiAlertService
{
    public BiAlert Add(ICollection<BiAlert> alerts, Guid accountId, string ruleCode, string severity, string reason, string sourceUrl, BiPeriod period)
    {
        period.Validate();
        if (alerts.Any(x => x.AccountId == accountId && x.RuleCode == ruleCode && x.Period == period && !x.Acknowledged))
            throw new InvalidOperationException("Já existe alerta aberto desta regra no período.");
        if (string.IsNullOrWhiteSpace(reason) || !sourceUrl.StartsWith('/')) throw new ArgumentException("Motivo e link interno real são obrigatórios.");
        var alert = new BiAlert(Guid.NewGuid(), accountId, ruleCode, severity, reason, sourceUrl, period, false);
        alerts.Add(alert); return alert;
    }
}

public sealed record BiDataMartSnapshot(Guid AccountId, BiPeriod Period, string SourceFingerprint, DateTimeOffset RefreshedAt);
public sealed class BiDataMartService
{
    public BiDataMartSnapshot Refresh(ICollection<BiDataMartSnapshot> snapshots, Guid accountId, BiPeriod period, string sourceFingerprint)
    {
        period.Validate();
        var existing = snapshots.FirstOrDefault(x => x.AccountId == accountId && x.Period == period && x.SourceFingerprint == sourceFingerprint);
        if (existing is not null) return existing;
        var snapshot = new BiDataMartSnapshot(accountId, period, sourceFingerprint, DateTimeOffset.UtcNow);
        snapshots.Add(snapshot); return snapshot;
    }
}

public sealed record BiForecast(decimal? Value, string Scenario, IReadOnlyList<string> Assumptions, bool HasSufficientData);
public sealed class BiForecastService
{
    public BiForecast Linear(IReadOnlyList<decimal> history, string scenario, IReadOnlyList<string> assumptions)
    {
        if (assumptions.Count == 0) throw new ArgumentException("O forecast exige premissas explícitas.");
        if (history.Count < 3) return new(null, scenario, assumptions, false);
        var changes = history.Zip(history.Skip(1), (a, b) => b - a).ToArray();
        return new(history[^1] + changes.Average(), scenario, assumptions, true);
    }
}

public sealed record BiDashboard(Guid Id, Guid AccountId, string Name, BiPeriod Period, IReadOnlyList<string> MetricCodes);
public sealed class BiDashboardService
{
    public IReadOnlyList<BiDashboard> ForAccount(IEnumerable<BiDashboard> dashboards, Guid accountId) => dashboards.Where(x => x.AccountId == accountId).ToArray();
}

public sealed record BiInsight(Guid AccountId, string Reason, string SourceUrl, string RecommendedAction);
public sealed class BiInsightService
{
    public BiInsight Create(Guid accountId, string reason, string sourceUrl, string action)
    {
        if (accountId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || !sourceUrl.StartsWith('/'))
            throw new ArgumentException("O insight exige conta, origem real e motivo.");
        return new(accountId, reason, sourceUrl, action);
    }
}
