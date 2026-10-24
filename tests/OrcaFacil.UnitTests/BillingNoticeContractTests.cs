using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Billing;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

// ============================================================================
// CONTRATO DOS AVISOS TRANSACIONAIS DE COBRANÇA (e-mail transacional)
//
// O BillingStatusService emite aviso APENAS por transição REAL de status —
// nunca por varredura que não mudou nada — e somente DEPOIS de persistir o
// estado (nada de e-mail contando status que não foi salvo). O destinatário
// concreto e o dedup por (evento, assinatura, dia) são da implementação Web
// (outbox com índice único de IdempotencyKey).
// ============================================================================

public sealed class BillingNoticeContractTests
{
    private static readonly DateTime Base = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Notice_follows_only_real_transitions_and_only_after_persistence()
    {
        var subscriptions = new InMemoryRepository<Subscription>();
        var eventRepo = new InMemoryRepository<SubscriptionEvent>();
        var notifier = new RecordingNotifier();

        var subscription = new Subscription
        {
            AccountId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BillingCycle = "monthly",
            Plan = PlanType.Professional,
            Status = SubscriptionStatus.Active,
            PaidThroughAt = Base.AddDays(-2),
            NextDueAt = Base.AddDays(-2)
        };
        await subscriptions.AddAsync(subscription);

        var clock = new TestClock(Base);
        var options = Options.Create(new BillingOptions { GracePeriodDays = 3, SuspendAfterDays = 5 });
        var billing = new BillingStatusService(subscriptions, new InMemoryRepository<Payment>(),
            new InMemoryRepository<BillingInvoice>(), eventRepo, new NoopUnitOfWork(), options, clock, notifier);

        // 1. Transição para PastDue: um aviso, depois da persistência.
        var processed = await billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(1, processed);
        var overdue = Assert.Single(notifier.Calls);
        Assert.Equal("billing.overdue", overdue.EventType);
        Assert.Equal(subscription.AccountId, notifier.Calls.Single().Subscription.AccountId);

        // 2. Varredura sem mudança: nenhum aviso novo (sem spam do worker).
        await billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(1, notifier.Calls.Count);

        // 3. Suspensão: segundo aviso.
        clock.UtcNow = Base.AddDays(4);
        await billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(SubscriptionStatus.Suspended, subscription.Status);
        Assert.Equal(2, notifier.Calls.Count);
        Assert.Equal("billing.suspended", notifier.Calls[^1].EventType);

        // 4. Regularização com cobertura comprovada: restauração notificada.
        clock.UtcNow = Base.AddDays(8);
        subscription.PaidThroughAt = clock.UtcNow.AddDays(30);
        subscription.NextDueAt = subscription.PaidThroughAt;
        await billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(3, notifier.Calls.Count);
        Assert.Equal("billing.restored", notifier.Calls[^1].EventType);

        // 5. Estado estável: nenhuma varredura nova gera aviso.
        await billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(3, notifier.Calls.Count);

        // Trilha de eventos persistida foi a fonte da verdade dos avisos: cada aviso tem um evento correspondente.
        foreach (var call in notifier.Calls)
        {
            var persisted = Assert.Single(eventRepo.Query().Where(x => x.EventType == call.EventType && x.SubscriptionId == call.Subscription.Id));
            Assert.Equal(call.EventType, persisted.EventType);
        }
    }

    [Fact]
    public async Task Manual_release_subscription_does_not_noise_the_owner_forever()
    {
        var subscriptions = new InMemoryRepository<Subscription>();
        var eventRepo = new InMemoryRepository<SubscriptionEvent>();
        var notifier = new RecordingNotifier();

        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new Subscription
        {
            AccountId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BillingCycle = "monthly",
            Status = SubscriptionStatus.PastDue,
            ManualReleaseUntil = now.AddDays(10),
            PastDueSince = now.AddDays(-6)
        };
        await subscriptions.AddAsync(subscription);

        var billing = new BillingStatusService(subscriptions, new InMemoryRepository<Payment>(),
            new InMemoryRepository<BillingInvoice>(), eventRepo, new NoopUnitOfWork(),
            Options.Create(new BillingOptions()), new TestClock(now), notifier);

        await billing.SyncOverdueSubscriptionsAsync();
        Assert.Equal(SubscriptionStatus.ManualRelease, subscription.Status);

        // Liberação manual MANDATÓRIA FEITA PELA MNSOFT não vira e-mail de cobrança:
        // o aviso de liberação é da operação humana, não do ciclo automático.
        Assert.DoesNotContain(notifier.Calls, call => call.EventType == "billing.manual_release");
    }

    private sealed record RecordedCall(Guid AccountId, string EventType, string Details, Subscription Subscription);

    private sealed class RecordingNotifier : IBillingNoticeService
    {
        public List<RecordedCall> Calls { get; } = [];

        public Task NotifyAsync(Subscription subscription, string eventType, string details, CancellationToken ct = default)
        {
            Calls.Add(new(subscription.AccountId!.Value, eventType, details, subscription));
            return Task.CompletedTask;
        }
    }
}
