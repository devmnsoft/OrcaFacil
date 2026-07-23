using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Plans;

public class PlanEntitlementService
{
    private const int FreeDocumentLimit = 20;
    private const int FreePdfLimit = 20;
    public Task<bool> CanCreateDocumentAsync(Guid userId, PlanType plan, SubscriptionStatus status, int monthlyDocuments = 0, CancellationToken ct = default) => Task.FromResult(IsProActive(plan, status) || monthlyDocuments < FreeDocumentLimit);
    public Task<bool> CanGeneratePdfAsync(Guid userId, PlanType plan, SubscriptionStatus status, int monthlyPdfs = 0, CancellationToken ct = default) => Task.FromResult(IsProActive(plan, status) || monthlyPdfs < FreePdfLimit);
    public Task<bool> CanUsePublicApprovalAsync(Guid userId, PlanType plan, SubscriptionStatus status, CancellationToken ct = default) => Task.FromResult(IsProActive(plan, status));
    public Task<bool> CanRemoveWatermarkAsync(Guid userId, PlanType plan, SubscriptionStatus status, CancellationToken ct = default) => Task.FromResult(IsProActive(plan, status));
    public Task<bool> CanUseAdvancedTemplatesAsync(Guid userId, PlanType plan, SubscriptionStatus status, CancellationToken ct = default) => Task.FromResult(IsProActive(plan, status));
    public Task<PlanEntitlementsDto> GetCurrentEntitlementsAsync(Guid userId, PlanType plan, SubscriptionStatus status, CancellationToken ct = default) => Task.FromResult(new PlanEntitlementsDto(IsProActive(plan,status), IsProActive(plan,status) ? null : FreeDocumentLimit, IsProActive(plan,status) ? null : FreePdfLimit, IsProActive(plan,status)));
    private static bool IsProActive(PlanType plan, SubscriptionStatus status) => plan == PlanType.Pro && (status == SubscriptionStatus.Active || status == SubscriptionStatus.Trial || status == SubscriptionStatus.ManualRelease);
}
public record PlanEntitlementsDto(bool ProBenefitsEnabled, int? MonthlyDocumentLimit, int? MonthlyPdfLimit, bool PublicApprovalEnabled);
