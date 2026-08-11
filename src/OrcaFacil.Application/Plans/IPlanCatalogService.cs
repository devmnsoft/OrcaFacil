namespace OrcaFacil.Application.Plans;

/// <summary>Public, read-only source of truth for every plan surface.</summary>
public interface IPlanCatalogService
{
    Task<PlanCatalogView> GetPublishedAsync(CancellationToken ct = default);
}

public sealed record PlanCatalogView(IReadOnlyList<PlanCardView> Plans, DateTime GeneratedAtUtc, bool IsFallback)
{
    public IReadOnlyList<PlanCardView> Summary => Plans.OrderBy(x => x.DisplayOrder).Take(3).ToArray();
}

public sealed record PlanCardView(string Code, string Name, string Description, decimal MonthlyPrice,
    decimal AnnualPrice, string Currency, bool IsRecommended, int DisplayOrder,
    IReadOnlyList<PlanFeatureView> Features, IReadOnlyList<PlanLimitView> Limits)
{
    public decimal AnnualSavings => Math.Max(0, MonthlyPrice * 12 - AnnualPrice);
}

public sealed record PlanFeatureView(string Code, string Name, string Description, string Category);
public sealed record PlanLimitView(string Code, string Name, int? Limit, bool IsUnlimited)
{
    public string DisplayValue => IsUnlimited || Limit is null ? "Sem limite definido" : Limit.Value.ToString("N0");
}
