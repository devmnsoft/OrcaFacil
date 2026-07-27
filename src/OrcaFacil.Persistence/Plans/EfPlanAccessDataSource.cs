using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Plans;

/// <summary>Database-backed source of truth for plan authorization and account usage.</summary>
public sealed class EfPlanAccessDataSource(OrcaFacilDbContext db) : IPlanAccessDataSource
{
    public Task<Subscription?> GetSubscriptionAsync(Guid accountId, CancellationToken ct) => db.Subscriptions
        .AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted)
        .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);

    public Task<PlanOverride?> GetActiveOverrideAsync(Guid accountId, DateTime utcNow, CancellationToken ct) => db.PlanOverrides
        .AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted && x.RevokedAt == null &&
                                  x.StartsAt <= utcNow && x.EndsAt > utcNow && x.Reason != "")
        .OrderByDescending(x => x.StartsAt).FirstOrDefaultAsync(ct);

    public Task<PlanVersion?> GetPlanVersionAsync(Guid versionId, CancellationToken ct) => db.PlanVersions
        .AsNoTracking().SingleOrDefaultAsync(x => x.Id == versionId && !x.IsDeleted, ct);

    public Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct) => db.Plans
        .AsNoTracking().SingleOrDefaultAsync(x => x.Id == planId && x.IsActive && !x.IsDeleted, ct);

    public Task<PlanVersion?> GetPublishedFreeVersionAsync(DateTime utcNow, CancellationToken ct) =>
        (from version in db.PlanVersions.AsNoTracking()
         join plan in db.Plans.AsNoTracking() on version.PlanId equals plan.Id
         where plan.Code == "FREE" && plan.IsActive && !plan.IsDeleted && !version.IsDeleted &&
               version.Status == PlanVersionStatus.Published && version.ValidFrom <= utcNow &&
               (version.ValidUntil == null || version.ValidUntil > utcNow)
         orderby version.VersionNumber descending
         select version).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetFeaturesAsync(Guid planVersionId, CancellationToken ct) =>
        await (from value in db.PlanFeatureValues.AsNoTracking()
               join feature in db.Features.AsNoTracking() on value.FeatureId equals feature.Id
               where value.PlanVersionId == planVersionId && feature.IsActive && !feature.IsDeleted && !value.IsDeleted
               select new { feature.Code, value.BooleanValue, value.IntegerValue, value.IsUnlimited })
            .ToDictionaryAsync(x => x.Code, x => new PlanFeatureSetting(x.BooleanValue, x.IntegerValue, x.IsUnlimited), ct);

    public async Task<int> GetUsageAsync(Guid accountId, string featureCode, DateTime periodStartUtc, CancellationToken ct)
    {
        var periodEndUtc = periodStartUtc.AddMonths(1);
        var period = periodStartUtc.ToString("yyyy-MM");
        return featureCode switch
        {
            "clients.active_limit" => await db.Clients.CountAsync(x => x.AccountId == accountId && !x.IsDeleted, ct),
            "services.active_limit" => await db.ActivityEvents.CountAsync(x => x.AccountId == accountId && !x.IsDeleted &&
                x.EntityType == "ServiceCatalogItem" && x.Action == "ServiceActivated" && x.Result == "Success", ct),
            "documents.monthly_limit" => await db.Documents.CountAsync(x => x.AccountId == accountId && !x.IsDeleted &&
                x.CreatedAt >= periodStartUtc && x.CreatedAt < periodEndUtc, ct),
            "pdf.monthly_limit" => await db.UserUsage.Where(x => x.AccountId == accountId && !x.IsDeleted &&
                x.Period == period).SumAsync(x => x.PdfGenerated, ct),
            "team.members_limit" => await db.AccountMembers.CountAsync(x => x.AccountId == accountId && !x.IsDeleted &&
                x.Status == AccountMemberStatus.Active, ct),
            "public_approval.monthly_limit" => await (from quote in db.PublicQuotes
                                                       join document in db.Documents on quote.DocumentId equals document.Id
                                                       where document.AccountId == accountId && !quote.IsDeleted && !document.IsDeleted &&
                                                             quote.CreatedAt >= periodStartUtc && quote.CreatedAt < periodEndUtc
                                                       select quote).CountAsync(ct),
            "templates.basic_limit" => await db.BudgetTemplates.CountAsync(x => !x.IsDeleted && x.IsActive &&
                (x.IsSystemTemplate || x.AccountId == accountId), ct),
            _ => 0
        };
    }

}
