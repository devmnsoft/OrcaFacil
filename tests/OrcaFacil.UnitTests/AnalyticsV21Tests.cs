using OrcaFacil.Application.Analytics;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class AnalyticsV21Tests
{
    [Fact]
    public void Comparison_without_previous_base_does_not_divide_by_zero()
    {
        var result = PeriodComparisonService.Compare(125, 0);
        Assert.Null(result.PercentageChange);
        Assert.Equal("Sem base comparativa", result.Label);
    }

    [Fact]
    public void Previous_equivalent_has_the_same_number_of_days()
    {
        var current = (Start: new DateOnly(2026, 8, 1), End: new DateOnly(2026, 8, 20));
        var previous = PeriodComparisonService.PreviousEquivalent(current.Start, current.End);
        Assert.Equal(current.End.DayNumber - current.Start.DayNumber, previous.End.DayNumber - previous.Start.DayNumber);
    }

    [Fact]
    public void Goal_rejects_invalid_period_and_negative_target()
    {
        var goal = new BusinessGoal { AccountId = Guid.NewGuid(), Name = "Receita", StartDate = new(2026, 8, 20), EndDate = new(2026, 8, 1), TargetValue = -1 };
        Assert.Throws<InvalidOperationException>(goal.Validate);
    }

    [Fact]
    public void Goal_progress_is_derived_from_value_and_elapsed_time()
    {
        var goal = new BusinessGoal { AccountId = Guid.NewGuid(), Name = "Receita", StartDate = new(2026, 8, 1), EndDate = new(2026, 8, 31), TargetValue = 1000 };
        var result = GoalProgressService.Calculate(goal, 800, new(2026, 8, 20));
        Assert.Equal(80, result.Percentage);
        Assert.Equal(GoalStatus.OnTrack, result.Status);
        Assert.Equal(11, result.RemainingDays);
    }

    [Fact]
    public void Forecast_is_deterministic_and_reports_low_confidence_without_history()
    {
        var input = new[] { (Value: 1000m, Score: 50, HasHistory: false), (Value: 500m, Score: 20, HasHistory: false) };
        var first = ForecastService.Calculate(input); var second = ForecastService.Calculate(input);
        Assert.Equal(600, first.WeightedValue);
        Assert.Equal("Baixa", first.Confidence);
        Assert.Equal(first, second);
    }
}
