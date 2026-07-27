namespace OrcaFacil.Application.Plans;

public static class PlanCatalogDefinitions
{
    public static readonly IReadOnlyDictionary<string, (string Name, decimal Monthly, decimal Annual)> Plans = new Dictionary<string, (string, decimal, decimal)>
    { ["FREE"] = ("Grátis", 0m, 0m), ["PROFESSIONAL"] = ("Profissional", 24.90m, 249m), ["BUSINESS"] = ("Negócio", 49.90m, 499m) };
    private static PlanFeatureSetting Yes => new(true, null, true);
    private static PlanFeatureSetting No => new(false);
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, PlanFeatureSetting>> Features = new Dictionary<string, IReadOnlyDictionary<string, PlanFeatureSetting>>
    {
        ["FREE"] = new Dictionary<string, PlanFeatureSetting> { ["team.members_limit"] = new(true, 1), ["clients.active_limit"] = new(true, 25), ["services.active_limit"] = new(true, 20), ["pdf.monthly_limit"] = new(true, 10), ["pdf.watermark"] = new(true), ["history.days_visible"] = new(true, 90), ["templates.basic_limit"] = new(true, 3), ["templates.custom_enabled"] = No, ["commercial.pipeline"] = No, ["exports.csv"] = No },
        ["PROFESSIONAL"] = new Dictionary<string, PlanFeatureSetting> { ["team.members_limit"] = new(true, 1), ["clients.active_limit"] = Yes, ["services.active_limit"] = Yes, ["pdf.monthly_limit"] = Yes, ["pdf.watermark"] = No, ["branding.custom_logo"] = Yes, ["templates.custom_enabled"] = Yes, ["public_approval.enabled"] = Yes, ["sharing.whatsapp"] = Yes, ["reports.basic"] = Yes, ["commercial.pipeline"] = No, ["exports.csv"] = No },
        ["BUSINESS"] = new Dictionary<string, PlanFeatureSetting> { ["team.members_limit"] = new(true, 3), ["clients.active_limit"] = Yes, ["services.active_limit"] = Yes, ["pdf.monthly_limit"] = Yes, ["pdf.watermark"] = No, ["branding.custom_logo"] = Yes, ["templates.custom_enabled"] = Yes, ["public_approval.enabled"] = Yes, ["sharing.whatsapp"] = Yes, ["reports.basic"] = Yes, ["reports.advanced"] = Yes, ["commercial.pipeline"] = Yes, ["commercial.followups"] = Yes, ["commercial.metrics"] = Yes, ["exports.csv"] = Yes, ["audit.account"] = Yes }
    };
}
