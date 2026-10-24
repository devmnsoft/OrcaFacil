using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Application.Plans;

/// <summary>
/// Matriz de referência do catálogo de planos. É a fonte de verdade usada como
/// fallback quando a base ainda não provisionou o recurso para a versão do plano
/// (ver <see cref="PlanAccessService"/>) e quando o catálogo público não pode ser
/// lido do banco (ver EfPlanCatalogService).
/// </summary>
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

    /// <summary>Recurso sem teto (habilitado e ilimitado).</summary>
    public static PlanFeatureSetting Unlimited { get; } = new(true, null, true);

    /// <summary>Recurso habilitado sem contagem (plano com acesso completo).</summary>
    public static PlanFeatureSetting Enabled { get; } = new(true);

    /// <summary>Recurso não incluído no plano.</summary>
    public static PlanFeatureSetting Disabled { get; } = new(false);

    /// <summary>Recurso habilitado com um teto numérico.</summary>
    public static PlanFeatureSetting Limit(int value) => new(true, value);

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, PlanFeatureSetting>> Features =
        new Dictionary<string, IReadOnlyDictionary<string, PlanFeatureSetting>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FREE"] = new Dictionary<string, PlanFeatureSetting>(StringComparer.OrdinalIgnoreCase)
            {
                [PlanFeatureCodes.TeamMembersLimit] = Limit(1),
                [PlanFeatureCodes.ClientsActiveLimit] = Limit(25),
                [PlanFeatureCodes.ServicesActiveLimit] = Limit(20),
                [PlanFeatureCodes.DocumentsMonthlyLimit] = Limit(20),
                [PlanFeatureCodes.PdfMonthlyLimit] = Limit(10),
                [PlanFeatureCodes.PdfWatermark] = Enabled,
                [PlanFeatureCodes.HistoryDaysVisible] = Limit(90),
                [PlanFeatureCodes.BasicTemplatesLimit] = Limit(3),
                [PlanFeatureCodes.CustomTemplatesEnabled] = Disabled,
                [PlanFeatureCodes.CustomBrandingEnabled] = Disabled,
                [PlanFeatureCodes.NumberingCustomPrefix] = Disabled,
                [PlanFeatureCodes.PublicApprovalEnabled] = Disabled,
                [PlanFeatureCodes.PublicApprovalMonthlyLimit] = Disabled,
                [PlanFeatureCodes.WhatsAppSharingEnabled] = Disabled,
                [PlanFeatureCodes.PublicLinkSharingEnabled] = Disabled,
                [PlanFeatureCodes.WorkOrdersEnabled] = Disabled,
                [PlanFeatureCodes.ManualPaymentsEnabled] = Disabled,
                [PlanFeatureCodes.OperationalReceiptsEnabled] = Enabled,
                [PlanFeatureCodes.DocumentConvertToReceiptEnabled] = Enabled,
                [PlanFeatureCodes.BasicReportsEnabled] = Disabled,
                [PlanFeatureCodes.AdvancedReportsEnabled] = Disabled,
                [PlanFeatureCodes.CommercialPipelineEnabled] = Disabled,
                [PlanFeatureCodes.CommercialFollowUpsEnabled] = Disabled,
                [PlanFeatureCodes.CommercialMetricsEnabled] = Disabled,
                [PlanFeatureCodes.CsvExportEnabled] = Disabled,
                [PlanFeatureCodes.AccountAuditEnabled] = Disabled,
                [PlanFeatureCodes.SupportPriorityEnabled] = Disabled,
                [PlanFeatureCodes.AiExternalProvidersEnabled] = Disabled,
                [PlanFeatureCodes.AiMonthlyLimit] = Limit(10)
            },
            ["PROFESSIONAL"] = new Dictionary<string, PlanFeatureSetting>(StringComparer.OrdinalIgnoreCase)
            {
                [PlanFeatureCodes.TeamMembersLimit] = Limit(1),
                [PlanFeatureCodes.ClientsActiveLimit] = Unlimited,
                [PlanFeatureCodes.ServicesActiveLimit] = Unlimited,
                [PlanFeatureCodes.DocumentsMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.PdfMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.PdfWatermark] = Disabled,
                [PlanFeatureCodes.HistoryDaysVisible] = Unlimited,
                [PlanFeatureCodes.BasicTemplatesLimit] = Unlimited,
                [PlanFeatureCodes.CustomTemplatesEnabled] = Enabled,
                [PlanFeatureCodes.CustomBrandingEnabled] = Enabled,
                [PlanFeatureCodes.NumberingCustomPrefix] = Enabled,
                [PlanFeatureCodes.PublicApprovalEnabled] = Enabled,
                [PlanFeatureCodes.PublicApprovalMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.WhatsAppSharingEnabled] = Enabled,
                [PlanFeatureCodes.PublicLinkSharingEnabled] = Enabled,
                [PlanFeatureCodes.WorkOrdersEnabled] = Enabled,
                [PlanFeatureCodes.ManualPaymentsEnabled] = Enabled,
                [PlanFeatureCodes.OperationalReceiptsEnabled] = Enabled,
                [PlanFeatureCodes.DocumentConvertToReceiptEnabled] = Enabled,
                [PlanFeatureCodes.BasicReportsEnabled] = Enabled,
                [PlanFeatureCodes.AdvancedReportsEnabled] = Disabled,
                [PlanFeatureCodes.CommercialPipelineEnabled] = Disabled,
                [PlanFeatureCodes.CommercialFollowUpsEnabled] = Disabled,
                [PlanFeatureCodes.CommercialMetricsEnabled] = Disabled,
                [PlanFeatureCodes.CsvExportEnabled] = Disabled,
                [PlanFeatureCodes.AccountAuditEnabled] = Disabled,
                [PlanFeatureCodes.SupportPriorityEnabled] = Disabled,
                [PlanFeatureCodes.AiExternalProvidersEnabled] = Enabled,
                [PlanFeatureCodes.AiMonthlyLimit] = Limit(200)
            },
            ["BUSINESS"] = new Dictionary<string, PlanFeatureSetting>(StringComparer.OrdinalIgnoreCase)
            {
                [PlanFeatureCodes.TeamMembersLimit] = Limit(3),
                [PlanFeatureCodes.ClientsActiveLimit] = Unlimited,
                [PlanFeatureCodes.ServicesActiveLimit] = Unlimited,
                [PlanFeatureCodes.DocumentsMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.PdfMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.PdfWatermark] = Disabled,
                [PlanFeatureCodes.HistoryDaysVisible] = Unlimited,
                [PlanFeatureCodes.BasicTemplatesLimit] = Unlimited,
                [PlanFeatureCodes.CustomTemplatesEnabled] = Enabled,
                [PlanFeatureCodes.CustomBrandingEnabled] = Enabled,
                [PlanFeatureCodes.NumberingCustomPrefix] = Enabled,
                [PlanFeatureCodes.PublicApprovalEnabled] = Enabled,
                [PlanFeatureCodes.PublicApprovalMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.WhatsAppSharingEnabled] = Enabled,
                [PlanFeatureCodes.PublicLinkSharingEnabled] = Enabled,
                [PlanFeatureCodes.WorkOrdersEnabled] = Enabled,
                [PlanFeatureCodes.ManualPaymentsEnabled] = Enabled,
                [PlanFeatureCodes.OperationalReceiptsEnabled] = Enabled,
                [PlanFeatureCodes.DocumentConvertToReceiptEnabled] = Enabled,
                [PlanFeatureCodes.BasicReportsEnabled] = Enabled,
                [PlanFeatureCodes.AdvancedReportsEnabled] = Enabled,
                [PlanFeatureCodes.CommercialPipelineEnabled] = Enabled,
                [PlanFeatureCodes.CommercialFollowUpsEnabled] = Enabled,
                [PlanFeatureCodes.CommercialMetricsEnabled] = Enabled,
                [PlanFeatureCodes.CsvExportEnabled] = Enabled,
                [PlanFeatureCodes.AccountAuditEnabled] = Enabled,
                [PlanFeatureCodes.SupportPriorityEnabled] = Enabled,
                [PlanFeatureCodes.AiExternalProvidersEnabled] = Enabled,
                [PlanFeatureCodes.AiMonthlyLimit] = Limit(1000)
            },
            ["ENTERPRISE"] = new Dictionary<string, PlanFeatureSetting>(StringComparer.OrdinalIgnoreCase)
            {
                [PlanFeatureCodes.TeamMembersLimit] = Limit(10),
                [PlanFeatureCodes.ClientsActiveLimit] = Unlimited,
                [PlanFeatureCodes.ServicesActiveLimit] = Unlimited,
                [PlanFeatureCodes.DocumentsMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.PdfMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.PdfWatermark] = Disabled,
                [PlanFeatureCodes.HistoryDaysVisible] = Unlimited,
                [PlanFeatureCodes.BasicTemplatesLimit] = Unlimited,
                [PlanFeatureCodes.CustomTemplatesEnabled] = Enabled,
                [PlanFeatureCodes.CustomBrandingEnabled] = Enabled,
                [PlanFeatureCodes.NumberingCustomPrefix] = Enabled,
                [PlanFeatureCodes.PublicApprovalEnabled] = Enabled,
                [PlanFeatureCodes.PublicApprovalMonthlyLimit] = Unlimited,
                [PlanFeatureCodes.WhatsAppSharingEnabled] = Enabled,
                [PlanFeatureCodes.PublicLinkSharingEnabled] = Enabled,
                [PlanFeatureCodes.WorkOrdersEnabled] = Enabled,
                [PlanFeatureCodes.ManualPaymentsEnabled] = Enabled,
                [PlanFeatureCodes.OperationalReceiptsEnabled] = Enabled,
                [PlanFeatureCodes.DocumentConvertToReceiptEnabled] = Enabled,
                [PlanFeatureCodes.BasicReportsEnabled] = Enabled,
                [PlanFeatureCodes.AdvancedReportsEnabled] = Enabled,
                [PlanFeatureCodes.CommercialPipelineEnabled] = Enabled,
                [PlanFeatureCodes.CommercialFollowUpsEnabled] = Enabled,
                [PlanFeatureCodes.CommercialMetricsEnabled] = Enabled,
                [PlanFeatureCodes.CsvExportEnabled] = Enabled,
                [PlanFeatureCodes.AccountAuditEnabled] = Enabled,
                [PlanFeatureCodes.SupportPriorityEnabled] = Enabled,
                [PlanFeatureCodes.AiExternalProvidersEnabled] = Enabled,
                [PlanFeatureCodes.AiMonthlyLimit] = Unlimited
            }
        };

    /// <summary>Recursos de referência de um plano, ou vazio quando o código é desconhecido.</summary>
    public static IReadOnlyDictionary<string, PlanFeatureSetting> ForPlan(string? planCode) =>
        planCode is not null && Features.TryGetValue(planCode, out var settings)
            ? settings
            : new Dictionary<string, PlanFeatureSetting>();
}
