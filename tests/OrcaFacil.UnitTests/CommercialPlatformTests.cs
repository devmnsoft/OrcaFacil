using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialPlatformTests
{
    [Fact]
    public async Task Expired_paid_plan_falls_back_to_published_free_version_without_deleting_data()
    {
        var source = new FakePlanSource();
        source.Subscription = new Subscription { AccountId = source.AccountId, SelectedPlanVersionId = source.BusinessVersion.Id,
            EffectivePlanVersionId = source.BusinessVersion.Id, Plan = PlanType.Business, Status = SubscriptionStatus.Active,
            PaidThroughAt = DateTime.UtcNow.AddMinutes(-1) };

        var effective = await new PlanAccessService(source).GetEffectivePlanAsync(source.AccountId, DateTime.UtcNow);

        Assert.Equal("FREE", effective?.Code);
        Assert.Equal(PlanType.Business, source.Subscription.Plan);
    }

    [Fact]
    public async Task Active_override_resolves_to_real_plan_version_and_expires()
    {
        var source = new FakePlanSource();
        var now = DateTime.UtcNow;
        source.Subscription = new Subscription { AccountId = source.AccountId, SelectedPlanVersionId = source.FreeVersion.Id,
            EffectivePlanVersionId = source.FreeVersion.Id, Status = SubscriptionStatus.Free };
        source.Override = new PlanOverride { AccountId = source.AccountId, PlanVersionId = source.BusinessVersion.Id,
            Reason = "Liberação assistida", StartsAt = now.AddMinutes(-1), EndsAt = now.AddDays(7), GrantedByUserId = Guid.NewGuid() };
        var service = new PlanAccessService(source);

        Assert.Equal("BUSINESS", (await service.GetEffectivePlanAsync(source.AccountId, now))?.Code);
        Assert.Equal(source.BusinessVersion.Id, (await service.GetEffectivePlanVersionAsync(source.AccountId, now))?.Id);
        Assert.Equal("FREE", (await service.GetEffectivePlanAsync(source.AccountId, now.AddDays(8)))?.Code);
    }

    [Fact]
    public async Task Database_feature_limit_uses_real_account_usage()
    {
        var source = new FakePlanSource { Usage = 10 };
        source.Subscription = new Subscription { AccountId = source.AccountId, SelectedPlanVersionId = source.FreeVersion.Id,
            EffectivePlanVersionId = source.FreeVersion.Id, Status = SubscriptionStatus.Free };
        var decision = await new PlanAccessService(source).CanUseAsync(source.AccountId, "pdf.monthly_limit");
        Assert.False(decision.IsAllowed);
        Assert.Equal(10, decision.CurrentUsage);
        Assert.Equal("BUSINESS", decision.RequiredPlanCode);
        Assert.Equal("PlanLimitReached", decision.InternalReason);
    }

    [Fact]
    public async Task Account_outside_active_state_fails_closed()
    {
        var source = new FakePlanSource { AccountStatus = AccountStatus.Blocked };
        var decision = await new PlanAccessService(source).CanUseAsync(source.AccountId, "pdf.monthly_limit");

        Assert.False(decision.IsAllowed);
        Assert.Equal("AccountBlocked", decision.InternalReason);
    }

    [Fact]
    public void Account_block_requires_reason()
    {
        var account = new BusinessAccount();
        Assert.Throws<ArgumentException>(() => account.Block(" "));
        account.Block("Análise de segurança");
        Assert.Equal(AccountStatus.Blocked, account.Status);
    }

    private sealed class FakePlanSource : IPlanAccessDataSource
    {
        public Guid AccountId { get; } = Guid.NewGuid();
        public Plan FreePlan { get; } = new() { Code = "FREE", DisplayName = "Grátis" };
        public Plan BusinessPlan { get; } = new() { Code = "BUSINESS", DisplayName = "Negócio" };
        public PlanVersion FreeVersion { get; }
        public PlanVersion BusinessVersion { get; }
        public Subscription? Subscription { get; set; }
        public PlanOverride? Override { get; set; }
        public int Usage { get; set; }
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;

        public FakePlanSource()
        {
            FreeVersion = new PlanVersion { PlanId = FreePlan.Id, VersionNumber = 1, Status = PlanVersionStatus.Published };
            BusinessVersion = new PlanVersion { PlanId = BusinessPlan.Id, VersionNumber = 1, Status = PlanVersionStatus.Published };
        }

        public Task<Subscription?> GetSubscriptionAsync(Guid accountId, CancellationToken ct) => Task.FromResult(Subscription);
        public Task<PlanOverride?> GetActiveOverrideAsync(Guid accountId, DateTime now, CancellationToken ct) =>
            Task.FromResult(Override?.IsEffective(now) == true ? Override : null);
        public Task<PlanVersion?> GetPlanVersionAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<PlanVersion?>(id == BusinessVersion.Id ? BusinessVersion : id == FreeVersion.Id ? FreeVersion : null);
        public Task<Plan?> GetPlanAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Plan?>(id == BusinessPlan.Id ? BusinessPlan : id == FreePlan.Id ? FreePlan : null);
        public Task<PlanVersion?> GetPublishedFreeVersionAsync(DateTime now, CancellationToken ct) => Task.FromResult<PlanVersion?>(FreeVersion);
        public Task<AccountStatus?> GetAccountStatusAsync(Guid accountId, CancellationToken ct) =>
            Task.FromResult<AccountStatus?>(AccountStatus);
        public Task<IReadOnlyList<PlanFeatureCandidate>> GetPublicPlanCandidatesAsync(string featureCode, DateTime now, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PlanFeatureCandidate>>([new("BUSINESS", 30, new(true, null, true))]);
        public Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetFeaturesAsync(Guid versionId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, PlanFeatureSetting>>(new Dictionary<string, PlanFeatureSetting>
            { ["pdf.monthly_limit"] = versionId == FreeVersion.Id ? new(true, 10) : new(true, null, true) });
        public Task<int> GetUsageAsync(Guid accountId, string featureCode, DateTime periodStartUtc, CancellationToken ct) => Task.FromResult(Usage);
    }
}
