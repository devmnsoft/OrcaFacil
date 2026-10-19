using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Application.Plans;

public enum PlanFeatureReadinessStatus
{
    /// <summary>Recurso completo e já aplicado na jornada (gate de plano ativo).</summary>
    Ready,
    /// <summary>Recurso existe na interface/serviços, mas ainda não consulta o plano.</summary>
    Partial,
    /// <summary>Recurso declarado no catálogo sem superfície implementada.</summary>
    Missing,
    /// <summary>Recurso desativado de propósito no produto.</summary>
    Disabled
}

public sealed record PlanFeatureReadiness(string Code, string DisplayName, PlanFeatureReadinessStatus Status,
    IReadOnlyList<string> RequiredRoutes, IReadOnlyList<string> RequiredServices,
    IReadOnlyList<string> RequiredHandlers, IReadOnlyList<string> RequiredEntities,
    IReadOnlyList<string> RequiredPermissions, DateTime LastVerifiedAt, string? Reason = null);

public interface IPlanFeatureReadinessService
{
    IReadOnlyList<PlanFeatureReadiness> GetAll();
    void EnsurePublishable(IEnumerable<string> enabledFeatureCodes);
}

/// <summary>
/// Manifesto de prontidão dos recursos de plano. Mantém todos os códigos de
/// <see cref="PlanFeatureCodes"/> catalogados e sinaliza, com transparência,
/// quais já aplicam o gate do plano (<see cref="PlanFeatureReadinessStatus.Ready"/>)
/// e quais ainda dependem de ligação (<see cref="PlanFeatureReadinessStatus.Partial"/>).
/// </summary>
public sealed class PlanFeatureReadinessService : IPlanFeatureReadinessService
{
    private static readonly DateTime VerifiedAt = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);
    private static readonly IReadOnlyDictionary<string, PlanFeatureReadiness> Manifest = BuildManifest();

    public IReadOnlyList<PlanFeatureReadiness> GetAll() => Manifest.Values.OrderBy(x => x.Code, StringComparer.Ordinal).ToArray();

    public void EnsurePublishable(IEnumerable<string> enabledFeatureCodes)
    {
        var blocked = enabledFeatureCodes.Distinct(StringComparer.Ordinal)
            .Where(code => !Manifest.TryGetValue(code, out var item) ||
                           item.Status is PlanFeatureReadinessStatus.Missing or PlanFeatureReadinessStatus.Disabled)
            .ToArray();
        if (blocked.Length != 0)
            throw new InvalidOperationException($"Publicação bloqueada. Recursos não operacionais: {string.Join(", ", blocked)}.");
    }

    private static IReadOnlyDictionary<string, PlanFeatureReadiness> BuildManifest()
    {
        static string Name(string code) =>
            PlanFeatureCodes.DisplayNames.TryGetValue(code, out var name) ? name : code;

        static PlanFeatureReadiness Ready(string code, string route, string service, string handler, string entity, string permission) =>
            new(code, Name(code), PlanFeatureReadinessStatus.Ready, [route], [service], [handler], [entity], [permission], VerifiedAt);

        static PlanFeatureReadiness Partial(string code, string reason) =>
            new(code, Name(code), PlanFeatureReadinessStatus.Partial, [], [], [], [], ["account.member"], VerifiedAt, reason);

        return new[]
        {
            // Gates de plano já aplicados na jornada.
            Ready(PlanFeatureCodes.ClientsActiveLimit, "/Clients", "IClientWorkspaceService", "OnPost", "Client", "account.member"),
            Ready(PlanFeatureCodes.ServicesActiveLimit, "/Services", "IServiceCatalogApplicationService", "OnPost", "ServiceCatalogItem", "account.member"),
            Ready(PlanFeatureCodes.DocumentsMonthlyLimit, "/Documents", "IPlanAccessService", "CanUseAsync", "Document", "account.member"),
            Ready(PlanFeatureCodes.PdfMonthlyLimit, "/Documents", "IPdfService", "OnPostDownloadPdf", "UserUsage", "account.member"),
            Ready(PlanFeatureCodes.HistoryDaysVisible, "/Documents", "IPlanAccessService", "CanUseAsync", "Document", "account.member"),
            Ready(PlanFeatureCodes.PublicApprovalEnabled, "/p/{token}", "ICommercialJourneyService", "CreatePublicAccessAsync", "PublicDocumentAccess", "account.member"),
            Ready(PlanFeatureCodes.WorkOrdersEnabled, "/WorkOrders", "ICommercialJourneyService", "ConvertToWorkOrderAsync", "WorkOrder", "account.member"),
            Ready(PlanFeatureCodes.ManualPaymentsEnabled, "/Payments", "IManualPaymentRegistrationService", "RegisterAsync", "ManualPayment", "account.member"),
            Ready(PlanFeatureCodes.AiExternalProvidersEnabled, "/Assistant", "IPlanAccessService", "CanUseAsync", "AiUsageLog", "account.member"),
            Ready(PlanFeatureCodes.AiMonthlyLimit, "/Assistant", "IAiGovernanceService", "CanUseAsync", "AiUsageLog", "account.member"),

            // Superfícies implementadas que ainda não consultam o plano.
            Ready(PlanFeatureCodes.TeamMembersLimit, "/AccountAdmin/Users/New", "IPlanAccessService", "GetLimitAsync", "AccountMember", "account.member"),
            Ready(PlanFeatureCodes.PdfWatermark, "/Documents (PDF)", "QuestPdfDocumentService", "GenerateDocumentPdfAsync", "Document", "documents.generate"),
            Partial(PlanFeatureCodes.BasicTemplatesLimit, "Catálogo de modelos existe, mas a criação ainda não compara com o limite do plano."),
            Partial(PlanFeatureCodes.CustomTemplatesEnabled, "Modelos personalizados existem e ainda não exigem plano pago."),
            Partial(PlanFeatureCodes.CustomBrandingEnabled, "Logo próprio é aceito e ainda não exige plano pago."),
            Partial(PlanFeatureCodes.NumberingCustomPrefix, "Prefixo de numeração existe e ainda não exige plano pago."),
            Ready(PlanFeatureCodes.PublicApprovalMonthlyLimit, "/CommercialJourneyService", "IPlanAccessService", "CanUseAsync", "PublicDocumentAccess", "documents.generate"),
            Ready(PlanFeatureCodes.WhatsAppSharingEnabled, "/Receipts/Details", "IPlanAccessService", "CanUseAsync", "Receipt", "account.member"),
            Ready(PlanFeatureCodes.PublicLinkSharingEnabled, "/CommercialJourneyService", "IPlanAccessService", "CanUseAsync", "PublicDocumentAccess", "documents.generate"),
            Ready(PlanFeatureCodes.OperationalReceiptsEnabled, "/Receipts", "IPlanAccessService", "CanUseAsync", "Receipt", "account.member"),
            Ready(PlanFeatureCodes.DocumentConvertToReceiptEnabled, "/Receipts/Create", "IPlanAccessService", "CanUseAsync", "Receipt", "receipts.create"),
            Ready(PlanFeatureCodes.BasicReportsEnabled, "/Reports", "IPlanAccessService", "CanUseAsync", "IntelligenceReport", "account.member"),
            Ready(PlanFeatureCodes.AdvancedReportsEnabled, "/Analytics/Executive", "IPlanAccessService", "CanUseAsync", "AnalyticsDashboard", "account.member"),
            Ready(PlanFeatureCodes.CommercialPipelineEnabled, "/CommercialPipeline", "IPlanAccessService", "CanUseAsync", "CommercialDashboardView", "account.member"),
            Ready(PlanFeatureCodes.CommercialFollowUpsEnabled, "/CommercialRoutine", "IPlanAccessService", "CanUseAsync", "RoutineItem", "account.member"),
            Partial(PlanFeatureCodes.CommercialMetricsEnabled, "Indicadores comerciais existem e ainda não exigem plano pago."),
            Ready(PlanFeatureCodes.CsvExportEnabled, "/Exports", "IPlanAccessService", "CanUseAsync", "DataExport", "account.member"),
            Ready(PlanFeatureCodes.AccountAuditEnabled, "/Settings/Audit", "IPlanAccessService", "CanUseAsync", "AuditLog", "account.member"),
            Partial(PlanFeatureCodes.SupportPriorityEnabled, "Suporte prioritário ainda é apenas um rótulo comercial.")
        }.ToDictionary(x => x.Code, StringComparer.Ordinal);
    }
}
