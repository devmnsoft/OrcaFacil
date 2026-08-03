using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Application.Plans;

public enum PlanFeatureReadinessStatus { Ready, Partial, Missing, Disabled }
public sealed record PlanFeatureReadiness(string Code, string DisplayName, PlanFeatureReadinessStatus Status,
    IReadOnlyList<string> RequiredRoutes, IReadOnlyList<string> RequiredServices,
    IReadOnlyList<string> RequiredHandlers, IReadOnlyList<string> RequiredEntities,
    IReadOnlyList<string> RequiredPermissions, DateTime LastVerifiedAt, string? Reason = null);

public interface IPlanFeatureReadinessService
{
    IReadOnlyList<PlanFeatureReadiness> GetAll();
    void EnsurePublishable(IEnumerable<string> enabledFeatureCodes);
}

public sealed class PlanFeatureReadinessService : IPlanFeatureReadinessService
{
    private static readonly DateTime VerifiedAt = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
    private static readonly IReadOnlyDictionary<string, PlanFeatureReadiness> Manifest = BuildManifest();
    public IReadOnlyList<PlanFeatureReadiness> GetAll() => Manifest.Values.OrderBy(x => x.Code).ToArray();

    public void EnsurePublishable(IEnumerable<string> enabledFeatureCodes)
    {
        var blocked = enabledFeatureCodes.Distinct(StringComparer.Ordinal).Where(code =>
            !Manifest.TryGetValue(code, out var item) || item.Status != PlanFeatureReadinessStatus.Ready).ToArray();
        if (blocked.Length != 0) throw new InvalidOperationException($"Publicação bloqueada. Recursos não operacionais: {string.Join(", ", blocked)}.");
    }

    private static IReadOnlyDictionary<string, PlanFeatureReadiness> BuildManifest()
    {
        static PlanFeatureReadiness Ready(string code, string name, string route, string service, string handler, string entity, string permission) =>
            new(code, name, PlanFeatureReadinessStatus.Ready, [route], [service], [handler], [entity], [permission], VerifiedAt);
        return new[]
        {
            Ready(PlanFeatureCodes.ClientsActiveLimit,"Clientes ativos","/Clients","IClientWorkspaceService","OnPost","Client","account.member"),
            Ready(PlanFeatureCodes.ServicesActiveLimit,"Serviços ativos","/Services","IServiceCatalogApplicationService","OnPost","ServiceCatalogItem","account.member"),
            Ready(PlanFeatureCodes.PdfMonthlyLimit,"PDF mensal","/Documents","IPdfService","OnPostDownloadPdf","UserUsage","account.member"),
            Ready(PlanFeatureCodes.PublicApprovalEnabled,"Aprovação pública","/p/{token}","ICommercialJourneyService","CreatePublicAccessAsync","PublicDocumentAccess","account.member"),
            Ready(PlanFeatureCodes.WorkOrdersEnabled,"Ordens de serviço","/WorkOrders","ICommercialJourneyService","ConvertToWorkOrderAsync","WorkOrder","account.member"),
            Ready(PlanFeatureCodes.ManualPaymentsEnabled,"Pagamentos manuais","/Payments","IManualPaymentRegistrationService","RegisterAsync","ManualPayment","account.member"),
            Ready(PlanFeatureCodes.OperationalReceiptsEnabled,"Recibos operacionais","/Receipts","IReceiptApplicationService","CreateAsync","Receipt","account.member")
        }.ToDictionary(x => x.Code, StringComparer.Ordinal);
    }
}
