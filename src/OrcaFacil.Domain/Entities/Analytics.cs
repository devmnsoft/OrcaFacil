using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum GoalType { ApprovedRevenue, ReceivedRevenue, SentQuotes, ApprovedQuotes, ApprovalRate, NewClients, CompletedWorkOrders, IssuedReceipts, OverdueReduction, ActiveContracts }
public enum GoalPeriodType { Monthly, Quarterly, Yearly, Custom }
public enum GoalStatus { OnTrack, AtRisk, Achieved, Missed, Paused, Canceled }
public enum DataQualitySeverity { Critical, High, Medium, Low, Info }
public enum DataQualityFindingStatus { Open, Resolved }

public sealed class BusinessGoal : Entity
{
    public Guid AccountId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public GoalType GoalType { get; set; }
    public GoalPeriodType PeriodType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.OnTrack;
    public Guid CreatedByUserId { get; set; }

    public void Validate()
    {
        if (AccountId == Guid.Empty) throw new InvalidOperationException("A conta da meta é obrigatória.");
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException("O nome da meta é obrigatório.");
        if (TargetValue < 0) throw new InvalidOperationException("O valor alvo não pode ser negativo.");
        if (EndDate < StartDate) throw new InvalidOperationException("O período final deve ser posterior ao inicial.");
    }
}

public sealed class GoalProgressSnapshot : Entity
{
    public Guid AccountId { get; set; }
    public Guid GoalId { get; set; }
    public DateOnly ReferenceDate { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProgressPercentage { get; set; }
    public GoalStatus Status { get; set; }
}

public sealed class AnalyticsSnapshot : Entity
{
    public Guid AccountId { get; set; }
    public string Frequency { get; set; } = "Daily";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AnalyticsSnapshotItem : Entity
{
    public Guid AccountId { get; set; }
    public Guid SnapshotId { get; set; }
    public string MetricCode { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Explanation { get; set; }
}

public sealed class ForecastSnapshot : Entity
{
    public Guid AccountId { get; set; }
    public string ForecastType { get; set; } = string.Empty;
    public DateOnly ReferenceDate { get; set; }
    public int HorizonDays { get; set; }
    public decimal ForecastValue { get; set; }
    public string Confidence { get; set; } = "Dados insuficientes";
    public string Explanation { get; set; } = string.Empty;
}

public sealed class DataQualityFinding : Entity
{
    public Guid AccountId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public DataQualitySeverity Severity { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public DataQualityFindingStatus Status { get; set; } = DataQualityFindingStatus.Open;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public sealed class DashboardWidgetPreference : Entity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string WidgetCode { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsFavorite { get; set; }
    public string DefaultPeriod { get; set; } = "month";
    public string FiltersJson { get; set; } = "{}";
}
