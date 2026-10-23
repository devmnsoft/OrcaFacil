using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Billing;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.Plans;
using Xunit;

namespace OrcaFacil.UnitTests;

// ============================================================================
// HOMOLOGAÇÃO DO CICLO DE INADIMPLÊNCIA PAGA (Sprint C / R5 — evidência ponta a ponta)
//
// Junta o motor de prazos (BillingStatusService, que marca PastDue/Suspended no
// worker) à fronteira de ação (PlanAccessService, que libera ou nega benefícios),
// provando que o calendário de cobrança muda o plano efetivo de verdade:
//
//   Active -> PastDue (carência, benefícios ATIVOS) -> fim da carência (plano
//   efetivo cai para FREE; índice comercial negado) -> Suspended (benefícios
//   PAUSADOS) -> regularização (cobertura futura comprovada; worker restaura) ->
//   benefícios de volta no plano pago.
//
// A fonte de dados de plano usa PlanVersion.GracePeriodDays = 3, o mesmo valor de
// BillingOptions.GracePeriodDays provisionado no seed (hotfix
// hotfix_plan_grace_period_v76.sql): a carência anunciada no evento
// billing.overdue é a mesma carência efetiva na liberação de benefícios.
// ============================================================================

public sealed class DunningCycleHomologationTests
{
    private const string PaidFeature = PlanFeatureCodes.WorkOrdersEnabled;

    private static readonly DateTime Base = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Paid_dunning_cycle_runs_due_grace_suspension_and_automatic_restoration()
    {
        // Vencimento há 2 dias; carência de 3 dias (BillingOptions e plan_versions alinhados).
        var dueAt = Base.AddDays(-2);
        var cycle = await CreateCycle(dueAt, Base);
        var accountId = cycle.Subscription.AccountId!.Value;

        // ETAPA 1 — dentro da carência: PastDue com carência anunciada e benefícios ATIVOS.
        var processed = await cycle.Billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(1, processed);
        Assert.Equal(SubscriptionStatus.PastDue, cycle.Subscription.Status);
        Assert.Equal(dueAt, cycle.Subscription.PastDueSince);
        var overdueEvent = Assert.Single(cycle.Events.Query().Where(x => x.EventType == "billing.overdue"));
        Assert.Contains("carência de 3 dias", overdueEvent.Details);

        var inGraceDecision = await cycle.Access.CanUseAsync(accountId, PaidFeature);
        Assert.True(inGraceDecision.IsAllowed);
        Assert.Equal("PROFESSIONAL", inGraceDecision.CurrentPlanCode);

        // ETAPA 2 — carência vencida, aguardando suspensão: plano efetivo cai para FREE.
        cycle.Clock.UtcNow = Base.AddDays(2); // 4 dias de atraso, abaixo dos 5 de suspensão
        var stillPastDue = await cycle.Billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(0, stillPastDue);
        Assert.Equal(SubscriptionStatus.PastDue, cycle.Subscription.Status);

        var afterGraceDecision = await cycle.Access.CanUseAsync(accountId, PaidFeature);
        Assert.False(afterGraceDecision.IsAllowed);
        Assert.Equal("FREE", afterGraceDecision.CurrentPlanCode);

        // ETAPA 3 — prazo de regularização excedido: benefícios SUSPENSOS.
        cycle.Clock.UtcNow = Base.AddDays(4); // 6 dias de atraso
        await cycle.Billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(SubscriptionStatus.Suspended, cycle.Subscription.Status);
        Assert.NotNull(cycle.Subscription.SuspendedAt);
        Assert.Single(cycle.Events.Query().Where(x => x.EventType == "billing.suspended"));

        var suspendedDecision = await cycle.Access.CanUseAsync(accountId, PaidFeature);
        Assert.False(suspendedDecision.IsAllowed);
        Assert.Equal("FREE", suspendedDecision.CurrentPlanCode);

        // ETAPA 4 — regularização: pagamento aprovado estende a cobertura; o worker
        // restaura a assinatura e o plano pago volta a autorizar o benefício.
        cycle.Clock.UtcNow = Base.AddDays(8);
        cycle.Subscription.PaidThroughAt = cycle.Clock.UtcNow.AddDays(30);
        cycle.Subscription.NextDueAt = cycle.Subscription.PaidThroughAt;

        await cycle.Billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(SubscriptionStatus.Active, cycle.Subscription.Status);
        Assert.Null(cycle.Subscription.PastDueSince);
        Assert.Null(cycle.Subscription.SuspendedAt);
        Assert.Single(cycle.Events.Query().Where(x => x.EventType == "billing.restored"));

        var restoredDecision = await cycle.Access.CanUseAsync(accountId, PaidFeature);
        Assert.True(restoredDecision.IsAllowed);
        Assert.Equal("PROFESSIONAL", restoredDecision.CurrentPlanCode);
    }

    [Fact]
    public async Task Settlement_committed_by_webhook_is_never_regressed_by_the_worker()
    {
        // Simula o efeito de MercadoPagoWebhookProcessor.ApplyFullCoverage:
        // Status Active + cobertura futura (PaidThroughAt/NextDueAt estendidos).
        var settled = await CreateCycle(Base.AddDays(-6), Base);
        settled.Subscription.Status = SubscriptionStatus.Active;
        settled.Subscription.PaidThroughAt = Base.AddDays(30);
        settled.Subscription.NextDueAt = Base.AddDays(30);
        settled.Subscription.LastPaymentAt = Base;

        await settled.Billing.SyncOverdueSubscriptionsAsync();
        await settled.Billing.SyncOverdueSubscriptionsAsync();

        Assert.Equal(SubscriptionStatus.Active, settled.Subscription.Status);
        Assert.Equal(Base.AddDays(30), settled.Subscription.PaidThroughAt);
        Assert.Null(settled.Subscription.PastDueSince);
        Assert.Null(settled.Subscription.SuspendedAt);
        Assert.Empty(settled.Events.Query());

        var accountId = settled.Subscription.AccountId!.Value;
        var decision = await settled.Access.CanUseAsync(accountId, PaidFeature);
        Assert.True(decision.IsAllowed);
        Assert.Equal("PROFESSIONAL", decision.CurrentPlanCode);
    }

    [Fact]
    public async Task Grace_period_announced_matches_the_benefit_release_on_the_vencimento_day()
    {
        // Vencimento no exato instante da sincronização: o evento anuncia carência de
        // 3 dias e, no MESMO instante, o plano pago ainda libera o benefício.
        // Congela o registro da divergência entre BillingOptions (carência 3) e o
        // seed de plan_versions (carência 0) que tornava a carência fictícia.
        var dueAt = Base;
        var cycle = await CreateCycle(dueAt, Base);
        var accountId = cycle.Subscription.AccountId!.Value;

        await cycle.Billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(SubscriptionStatus.PastDue, cycle.Subscription.Status);
        var overdueEvent = Assert.Single(cycle.Events.Query().Where(x => x.EventType == "billing.overdue"));
        Assert.Contains("carência de 3 dias", overdueEvent.Details);

        var sameInstantDecision = await cycle.Access.CanUseAsync(accountId, PaidFeature);
        Assert.True(sameInstantDecision.IsAllowed);
        Assert.Equal("PROFESSIONAL", sameInstantDecision.CurrentPlanCode);
    }

    // =========================================================================
    // ACESSO DE PLANO ALINHADO AO MOTOR DE COBRANÇA (fonte de dados de teste)
    // =========================================================================

    /// <summary>Expõe o mesmo relógio do motor de cobrança ao serviço de acesso:
    /// sem isso a junção é testada com dois tempos diferentes.</summary>
    private sealed class TestClockTimeProvider(TestClock clock) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(clock.UtcNow, TimeSpan.Zero);
    }

    private static async Task<(Subscription Subscription, TestClock Clock, BillingStatusService Billing,
        PlanAccessService Access, InMemoryRepository<SubscriptionEvent> Events)> CreateCycle(
            DateTime dueAt, DateTime utcNow)
    {
        var subscription = new Subscription
        {
            AccountId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BillingCycle = "monthly",
            Plan = PlanType.Professional,
            Status = SubscriptionStatus.Active,
            SelectedPlanVersionId = DunningPlanAccessDataSource.PaidVersionId,
            PaidThroughAt = dueAt,
            NextDueAt = dueAt
        };

        var clock = new TestClock(utcNow);
        var options = Options.Create(new BillingOptions { GracePeriodDays = 3, SuspendAfterDays = 5 });

        var subscriptionRepo = new InMemoryRepository<Subscription>();
        await subscriptionRepo.AddAsync(subscription);
        var paymentRepo = new InMemoryRepository<Payment>();
        var invoiceRepo = new InMemoryRepository<BillingInvoice>();
        var eventsRepo = new InMemoryRepository<SubscriptionEvent>();

        var billing = new BillingStatusService(
            subscriptionRepo, paymentRepo, invoiceRepo, eventsRepo, new NoopUnitOfWork(), options, clock);

        var access = new PlanAccessService(new DunningPlanAccessDataSource(subscription), new TestClockTimeProvider(clock));

        return (subscription, clock, billing, access, eventsRepo);
    }
    internal sealed class DunningPlanAccessDataSource : IPlanAccessDataSource
    {
        private static readonly Plan PaidPlan = new() { Code = "PROFESSIONAL", DisplayName = "Profissional" };
        private static readonly Plan FreePlan = new() { Code = "FREE", DisplayName = "Grátis" };

        // Versões com o mesmo valor de carência provisionado nos bancos (v76):
        // 3 dias nos planos pagos, 0 no FREE.
        private static readonly PlanVersion PaidVersion = new()
        {
            PlanId = PaidPlan.Id,
            VersionNumber = 1,
            Status = PlanVersionStatus.Published,
            GracePeriodDays = 3
        };

        private static readonly PlanVersion FreeVersion = new()
        {
            PlanId = FreePlan.Id,
            VersionNumber = 1,
            Status = PlanVersionStatus.Published,
            GracePeriodDays = 0
        };

        public static readonly Guid PaidVersionId = PaidVersion.Id;
        public static readonly Guid FreeVersionId = FreeVersion.Id;

        private readonly Subscription _subscription;

        public DunningPlanAccessDataSource(Subscription subscription)
        {
            _subscription = subscription;
        }

        public Task<AccountStatus?> GetAccountStatusAsync(Guid accountId, CancellationToken ct) =>
            Task.FromResult<AccountStatus?>(accountId == _subscription.AccountId ? AccountStatus.Active : null);

        public Task<PlanOverride?> GetActiveOverrideAsync(Guid accountId, DateTime utcNow, CancellationToken ct) =>
            Task.FromResult<PlanOverride?>(null);

        public Task<Subscription?> GetSubscriptionAsync(Guid accountId, CancellationToken ct) =>
            accountId == _subscription.AccountId ? Task.FromResult<Subscription?>(_subscription) : Task.FromResult<Subscription?>(null);

        public Task<PlanVersion?> GetPlanVersionAsync(Guid versionId, CancellationToken ct)
        {
            if (versionId == PaidVersion.Id) return Task.FromResult<PlanVersion?>(PaidVersion);
            if (versionId == FreeVersion.Id) return Task.FromResult<PlanVersion?>(FreeVersion);
            return Task.FromResult<PlanVersion?>(null);
        }

        public Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct) =>
            planId == PaidPlan.Id ? Task.FromResult<Plan?>(PaidPlan)
            : planId == FreePlan.Id ? Task.FromResult<Plan?>(FreePlan)
            : Task.FromResult<Plan?>(null);

        public Task<PlanVersion?> GetPublishedFreeVersionAsync(DateTime utcNow, CancellationToken ct) =>
            Task.FromResult<PlanVersion?>(FreeVersion);

        // Sem recursos provisionados no banco: a matriz de referência
        // (PlanCatalogDefinitions) cobre — exatamente como o hotfix v76 deixa os
        // ambientes provisionados por hotfix_plan_feature_catalog_v75.sql.
        public Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetFeaturesAsync(Guid planVersionId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, PlanFeatureSetting>>(new Dictionary<string, PlanFeatureSetting>());

        public Task<int> GetUsageAsync(Guid accountId, string featureCode, DateTime periodStartUtc, CancellationToken ct) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<PlanFeatureCandidate>> GetPublicPlanCandidatesAsync(string featureCode, DateTime utcNow, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PlanFeatureCandidate>>([]);

        public Task<IReadOnlyDictionary<string, int>> GetAddonIncrementsAsync(Guid accountId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, int>>(new Dictionary<string, int>());
    }
}
