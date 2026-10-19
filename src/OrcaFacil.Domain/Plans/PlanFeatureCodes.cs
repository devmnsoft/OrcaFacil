namespace OrcaFacil.Domain.Plans;

public static class PlanFeatureCodes
{
    public const string TeamMembersLimit = "team.members_limit";
    public const string ClientsActiveLimit = "clients.active_limit";
    public const string ServicesActiveLimit = "services.active_limit";
    public const string DocumentsMonthlyLimit = "documents.monthly_limit";
    public const string PdfMonthlyLimit = "pdf.monthly_limit";
    public const string PdfWatermark = "pdf.watermark";
    public const string HistoryDaysVisible = "history.days_visible";
    public const string BasicTemplatesLimit = "templates.basic_limit";
    public const string CustomTemplatesEnabled = "templates.custom_enabled";
    public const string CustomBrandingEnabled = "branding.custom_logo";
    public const string NumberingCustomPrefix = "numbering.custom_prefix";
    public const string PublicApprovalEnabled = "public_approval.enabled";
    public const string PublicApprovalMonthlyLimit = "public_approval.monthly_limit";
    public const string WhatsAppSharingEnabled = "sharing.whatsapp";
    public const string PublicLinkSharingEnabled = "sharing.public_link";
    public const string WorkOrdersEnabled = "work_orders.enabled";
    public const string ManualPaymentsEnabled = "payments.manual.enabled";
    public const string OperationalReceiptsEnabled = "receipts.operational.enabled";
    public const string DocumentConvertToReceiptEnabled = "document.convert_to_receipt";
    public const string BasicReportsEnabled = "reports.basic";
    public const string AdvancedReportsEnabled = "reports.advanced";
    public const string CommercialPipelineEnabled = "commercial.pipeline";
    public const string CommercialFollowUpsEnabled = "commercial.followups";
    public const string CommercialMetricsEnabled = "commercial.metrics";
    public const string CsvExportEnabled = "exports.csv";
    public const string AccountAuditEnabled = "audit.account";
    public const string SupportPriorityEnabled = "support.priority";
    public const string AiExternalProvidersEnabled = "ai.external.enabled";
    public const string AiMonthlyLimit = "ai.monthly_limit";

    /// <summary>Todos os códigos de recurso reconhecidos pelo catálogo de planos.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        TeamMembersLimit, ClientsActiveLimit, ServicesActiveLimit, DocumentsMonthlyLimit,
        PdfMonthlyLimit, PdfWatermark, HistoryDaysVisible, BasicTemplatesLimit,
        CustomTemplatesEnabled, CustomBrandingEnabled, NumberingCustomPrefix,
        PublicApprovalEnabled, PublicApprovalMonthlyLimit, WhatsAppSharingEnabled, PublicLinkSharingEnabled,
        WorkOrdersEnabled, ManualPaymentsEnabled, OperationalReceiptsEnabled, DocumentConvertToReceiptEnabled,
        BasicReportsEnabled, AdvancedReportsEnabled, CommercialPipelineEnabled, CommercialFollowUpsEnabled,
        CommercialMetricsEnabled, CsvExportEnabled, AccountAuditEnabled, SupportPriorityEnabled,
        AiExternalProvidersEnabled, AiMonthlyLimit
    ];

    /// <summary>Nome de exibição em português usado quando o banco não tem o recurso cadastrado.</summary>
    public static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TeamMembersLimit] = "Pessoas da equipe",
            [ClientsActiveLimit] = "Clientes ativos",
            [ServicesActiveLimit] = "Serviços ativos",
            [DocumentsMonthlyLimit] = "Documentos mensais",
            [PdfMonthlyLimit] = "PDFs mensais",
            [PdfWatermark] = "Marca Fechô",
            [HistoryDaysVisible] = "Histórico visível",
            [BasicTemplatesLimit] = "Modelos básicos",
            [CustomTemplatesEnabled] = "Modelos personalizados",
            [CustomBrandingEnabled] = "Logo próprio",
            [NumberingCustomPrefix] = "Prefixo personalizado",
            [PublicApprovalEnabled] = "Aprovação pública",
            [PublicApprovalMonthlyLimit] = "Aprovações mensais",
            [WhatsAppSharingEnabled] = "Compartilhamento por WhatsApp",
            [PublicLinkSharingEnabled] = "Link público",
            [WorkOrdersEnabled] = "Ordens de serviço",
            [ManualPaymentsEnabled] = "Pagamentos manuais",
            [OperationalReceiptsEnabled] = "Recibos operacionais",
            [DocumentConvertToReceiptEnabled] = "Conversão em recibo",
            [BasicReportsEnabled] = "Relatórios básicos",
            [AdvancedReportsEnabled] = "Relatórios avançados",
            [CommercialPipelineEnabled] = "Pipeline comercial",
            [CommercialFollowUpsEnabled] = "Acompanhamentos",
            [CommercialMetricsEnabled] = "Indicadores comerciais",
            [CsvExportEnabled] = "Exportação CSV",
            [AccountAuditEnabled] = "Auditoria da conta",
            [SupportPriorityEnabled] = "Suporte prioritário",
            [AiExternalProvidersEnabled] = "IA com provedores externos",
            [AiMonthlyLimit] = "Operações de IA por mês"
        };
}
