namespace OrcaFacil.Web.ViewModels;

public sealed record PlanUsageSummaryViewModel(
    string SelectedPlan, string EffectivePlan, string Status,
    IReadOnlyList<PlanUsageItemViewModel> Items, DateTimeOffset? RenewsAt = null,
    string? NextBenefit = null);
public sealed record PlanUsageItemViewModel(string Label, int Used, int? Limit)
{
    public int Remaining => Limit.HasValue ? Math.Max(0, Limit.Value - Used) : 0;
    public int Percentage => Limit is > 0 ? Math.Min(100, (int)Math.Round(Used * 100d / Limit.Value)) : 0;
}
