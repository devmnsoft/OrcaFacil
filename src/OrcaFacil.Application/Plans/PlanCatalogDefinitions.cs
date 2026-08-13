using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Application.Plans;

public static class PlanCatalogDefinitions
{
    public static readonly IReadOnlyDictionary<string, (string Name, decimal Monthly, decimal Annual)> Plans =
        new Dictionary<string, (string, decimal, decimal)>(StringComparer.OrdinalIgnoreCase)
        {
            ["FREE"] = ("Grátis", 0m, 0m),
            ["PROFESSIONAL"] = ("Profissional", 24.90m, 249m),
            ["BUSINESS"] = ("Negócio", 49.90m, 499m),
            ["ENTERPRISE"] = ("Enterprise", 0m, 0m)
        };

    private static PlanFeatureSetting Unlimited => new(true, null, true);
    private static PlanFeatureSetting Enabled => new(true);
    private static PlanFeatureSetting Disabled => new(false);

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, PlanFeatureSetting>> Features =
        new Dictionary<string, IReadOnlyDictionary<string, PlanFeatureSetting>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FREE"] = Matrix(1, 25, 20, 10, true, 90, 3, false, false, false, false, false, false, true, false, false, false, false, false, false, false),
            ["PROFESSIONAL"] = Matrix(1, null, null, null, false, null, null, true, true, true, true, true, true, true, true, false, false, false, false, false, false),
            ["BUSINESS"] = Matrix(3, null, null, null, false, null, null, true, true, true, true, true, true, true, true, true, true, true, true, true, true),
            ["ENTERPRISE"] = Matrix(10, null, null, null, false, null, null, true, true, true, true, true, true, true, true, true, true, true, true, true, true)
        };

    private static IReadOnlyDictionary<string, PlanFeatureSetting> Matrix(int team, int? clients, int? services, int? pdf,
        bool watermark, int? history, int? templates, bool customTemplates, bool branding, bool approval, bool whatsapp,
        bool workOrders, bool payments, bool receipts, bool basicReports, bool advancedReports, bool pipeline,
        bool followups, bool metrics, bool csv, bool audit) => new Dictionary<string, PlanFeatureSetting>
    {
        [PlanFeatureCodes.TeamMembersLimit] = new(true, team),
        [PlanFeatureCodes.ClientsActiveLimit] = Limit(clients),
        [PlanFeatureCodes.ServicesActiveLimit] = Limit(services),
        [PlanFeatureCodes.PdfMonthlyLimit] = Limit(pdf),
        [PlanFeatureCodes.PdfWatermark] = watermark ? Enabled : Disabled,
        [PlanFeatureCodes.HistoryDaysVisible] = Limit(history),
        [PlanFeatureCodes.BasicTemplatesLimit] = Limit(templates),
        [PlanFeatureCodes.CustomTemplatesEnabled] = Flag(customTemplates),
        [PlanFeatureCodes.CustomBrandingEnabled] = Flag(branding),
        [PlanFeatureCodes.PublicApprovalEnabled] = Flag(approval),
        [PlanFeatureCodes.WhatsAppSharingEnabled] = Flag(whatsapp),
        [PlanFeatureCodes.WorkOrdersEnabled] = Flag(workOrders),
        [PlanFeatureCodes.ManualPaymentsEnabled] = Flag(payments),
        [PlanFeatureCodes.OperationalReceiptsEnabled] = Flag(receipts),
        [PlanFeatureCodes.BasicReportsEnabled] = Flag(basicReports),
        [PlanFeatureCodes.AdvancedReportsEnabled] = Flag(advancedReports),
        [PlanFeatureCodes.CommercialPipelineEnabled] = Flag(pipeline),
        [PlanFeatureCodes.CommercialFollowUpsEnabled] = Flag(followups),
        [PlanFeatureCodes.CommercialMetricsEnabled] = Flag(metrics),
        [PlanFeatureCodes.CsvExportEnabled] = Flag(csv),
        [PlanFeatureCodes.AccountAuditEnabled] = Flag(audit)
    };

    private static PlanFeatureSetting Limit(int? value) => value is null ? Unlimited : new(true, value);
    private static PlanFeatureSetting Flag(bool enabled) => enabled ? Enabled : Disabled;
}
