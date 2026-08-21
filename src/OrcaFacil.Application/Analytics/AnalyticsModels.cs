using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Analytics;

public sealed record PeriodComparison(decimal Current, decimal Previous, decimal? PercentageChange, string Label);
public sealed record GoalProgress(decimal CurrentValue, decimal Percentage, int ElapsedDays, int RemainingDays, decimal RequiredDailyPace, GoalStatus Status, string Explanation);
public sealed record ForecastResult(decimal WeightedValue, string Confidence, string Explanation);

public static class PeriodComparisonService
{
    public static PeriodComparison Compare(decimal current, decimal previous) => previous == 0
        ? new(current, previous, null, "Sem base comparativa")
        : new(current, previous, decimal.Round(((current - previous) / Math.Abs(previous)) * 100, 2), "Comparação com período anterior equivalente");

    public static (DateOnly Start, DateOnly End) PreviousEquivalent(DateOnly start, DateOnly end)
    {
        if (end < start) throw new ArgumentException("Período inválido.");
        var days = end.DayNumber - start.DayNumber + 1;
        return (start.AddDays(-days), start.AddDays(-1));
    }
}

public static class GoalProgressService
{
    public static GoalProgress Calculate(BusinessGoal goal, decimal current, DateOnly today)
    {
        goal.Validate();
        if (goal.Status is GoalStatus.Paused or GoalStatus.Canceled)
            return new(current, goal.TargetValue == 0 ? 100 : current / goal.TargetValue * 100, 0, 0, 0, goal.Status, "Meta pausada ou cancelada; ritmo não calculado.");
        var total = goal.EndDate.DayNumber - goal.StartDate.DayNumber + 1;
        var elapsed = Math.Clamp(today.DayNumber - goal.StartDate.DayNumber + 1, 0, total);
        var remaining = Math.Max(0, total - elapsed);
        var percentage = goal.TargetValue == 0 ? 100 : decimal.Round(current / goal.TargetValue * 100, 2);
        var expected = total == 0 ? 100 : elapsed * 100m / total;
        var status = percentage >= 100 ? GoalStatus.Achieved : today > goal.EndDate ? GoalStatus.Missed : percentage + 5 >= expected ? GoalStatus.OnTrack : GoalStatus.AtRisk;
        var pace = remaining == 0 ? 0 : Math.Max(0, goal.TargetValue - current) / remaining;
        return new(current, percentage, elapsed, remaining, decimal.Round(pace, 2), status, $"{percentage:0.##}% realizado; {remaining} dias restantes.");
    }
}

public static class ForecastService
{
    public static ForecastResult Calculate(IEnumerable<(decimal Value, int Score, bool HasHistory)> proposals)
    {
        var rows = proposals.ToArray();
        if (rows.Length == 0) return new(0, "Dados insuficientes", "Não há propostas abertas no período.");
        var weighted = rows.Sum(x => x.Value * Math.Clamp(x.Score, 0, 100) / 100m);
        var history = rows.Count(x => x.HasHistory);
        var confidence = history == 0 ? "Baixa" : history == rows.Length && rows.Length >= 5 ? "Alta" : "Média";
        return new(decimal.Round(weighted, 2), confidence, "Previsão determinística: valor aberto multiplicado pelo score comercial; a confiança considera a disponibilidade de histórico real.");
    }
}
