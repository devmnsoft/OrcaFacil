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
            Partial(PlanFeatureCodes.TeamMembersLimit, "Vínculos de equipe existem, mas o convite ainda não compara com o limite do plano."),
            Partial(PlanFeatureCodes.PdfWatermark, "Serviço de PDF aceita marca d'água, mas a geração ainda não aplica a marca do plano gratuito."),
            Partial(PlanFeatureCodes.BasicTemplatesLimit, "Catálogo de modelos existe, mas a criação ainda não compara com o limite do plano."),
            Partial(PlanFeatureCodes.CustomTemplatesEnabled, "Modelos personalizados existem e ainda não exigem plano pago."),
            Partial(PlanFeatureCodes.CustomBrandingEnabled, "Logo próprio é aceito e ainda não exige plano pago."),
            Partial(PlanFeatureCodes.NumberingCustomPrefix, "Prefixo de numeração existe e ainda não exige plano pago."),
            Partial(PlanFeatureCodes.PublicApprovalMonthlyLimit, "Aprovações são contadas no uso, mas o teto mensal ainda não bloqueia."),
            Partial(PlanFeatureCodes.WhatsAppSharingEnabled, "Compartilhamento por WhatsApp existe e ainda não exige plano pago."),
            Partial(PlanFeatureCodes.PublicLinkSharingEnabled, "Link público é liberado junto da aprovação pública, sem gate próprio."),
            Partial(PlanFeatureCodes.OperationalReceiptsEnabled, "Recibos existem e operam igualmente em todos os planos."),
            Partial(PlanFeatureCodes.DocumentConvertToReceiptEnabled, "Conversão em recibo existe e ainda não consulta o plano."),
            Ready(PlanFeatureCodes.BasicReportsEnabled, "/Reports", "IPlanAccessService", "CanUseAsync", "IntelligenceReport", "account.member"),
            Ready(PlanFeatureCodes.AdvancedReportsEnabled, "/Analytics/Executive", "IPlanAccessService", "CanUseAsync", "AnalyticsDashboard", "account.member"),
            Ready(PlanFeatureCodes.CommercialPipelineEnabled, "/CommercialPipeline", "IPlanAccessService", "CanUseAsync", "CommercialDashboardView", "account.member"),
            Ready(PlanFeatureCodes.CommercialFollowUpsEnabled, "/CommercialRoutine", "IPlanAccessService", "CanUseAsync", "RoutineItem", "account.member"),
            Partial(PlanFeatureCodes.CommercialMetricsEnabled, "Indicadores comerciais existem e ainda não exigem plano pago."),
            Ready(PlanFeatureCodes.CsvExportEnabled, "/Exports", "IPlanAccessService", "CanUseAsync", "DataExport", "account.member"),
            Partial(PlanFeatureCodes.AccountAuditEnabled, "Trilha de auditoria existe e ainda não exige plano pago."),
            Partial(PlanFeatureCodes.SupportPriorityEnabled, "Suporte prioritário ainda é apenas um rótulo comercial.")
        }.ToDictionary(x => x.Code, StringComparer.Ordinal);
    }
}
