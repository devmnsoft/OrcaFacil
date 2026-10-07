using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Services;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Billing;

public class BillingStatusService
{
    private readonly IRepository<Subscription> _subscriptions;
    private readonly IRepository<Payment> _payments;
    private readonly IRepository<BillingInvoice> _invoices;
    private readonly IRepository<SubscriptionEvent> _events;
    private readonly IUnitOfWork _uow;
    private readonly BillingOptions _options;
    private readonly IClock _clock;

    public BillingStatusService(
        IRepository<Subscription> subscriptions,
        IRepository<Payment> payments,
        IRepository<BillingInvoice> invoices,
        IRepository<SubscriptionEvent> events,
        IUnitOfWork uow,
        IOptions<BillingOptions>? options = null,
        IClock? clock = null)
    {
        _subscriptions = subscriptions;
        _payments = payments;
        _invoices = invoices;
        _events = events;
        _uow = uow;
        _options = options?.Value ?? new BillingOptions();
        _clock = clock ?? new SystemClock();
    }

    public async Task<int> SyncOverdueSubscriptionsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var query = _subscriptions.Query()
            .Where(x => x.AccountId.HasValue && x.AccountId != Guid.Empty &&
                x.Status != SubscriptionStatus.Free &&
                x.Status != SubscriptionStatus.Cancelled &&
                x.Status != SubscriptionStatus.Canceled &&
                x.Status != SubscriptionStatus.Expired)
            .OrderBy(x => x.Id);

        var processed = 0;
        const int pageSize = 50;
        var page = 0;

        while (!ct.IsCancellationRequested)
        {
            var batch = query.Skip(page * pageSize).Take(pageSize).ToList();
            if (batch.Count == 0) break;

            foreach (var subscription in batch)
            {
                ct.ThrowIfCancellationRequested();

                // 1. Separate manual release
                if (subscription.ManualReleaseUntil.HasValue && subscription.ManualReleaseUntil.Value > now)
                {
                    if (subscription.Status != SubscriptionStatus.ManualRelease && subscription.Status != SubscriptionStatus.Active)
                    {
                        subscription.Status = SubscriptionStatus.ManualRelease;
                        await RegisterEventAsync(subscription, "billing.manual_release", $"Liberação manual até {subscription.ManualReleaseUntil.Value:O}.", ct);
                        processed++;
                    }
                    continue;
                }

                // 2. Separate active trial
                if (subscription.TrialStatus == TrialStatus.Active && subscription.TrialEndsAt.HasValue && subscription.TrialEndsAt.Value > now)
                {
                    if (subscription.Status != SubscriptionStatus.Trial && subscription.Status != SubscriptionStatus.Trialing)
                    {
                        subscription.Status = SubscriptionStatus.Trial;
                        await RegisterEventAsync(subscription, "billing.trial_active", "Trial em vigor.", ct);
                        processed++;
                    }
                    continue;
                }

                var dueAt = subscription.NextDueAt ?? subscription.PaidThroughAt ?? subscription.ExpiresAt;
                if (!dueAt.HasValue) continue;

                // 3. Quittance check for the cycle obligation
                var cycleSettled = await HasProvenSettlementForCycleAsync(subscription, dueAt.Value, now, ct);
                if (cycleSettled)
                {
                    if (subscription.Status is SubscriptionStatus.PastDue or SubscriptionStatus.Suspended
                        or SubscriptionStatus.PendingPayment or SubscriptionStatus.PendingActivation)
                    {
                        RestoreSubscription(subscription);
                        await RegisterEventAsync(subscription, "billing.restored", "Ciclo quitado com comprovação; benefícios ativos.", ct);
                        processed++;
                    }
                    continue;
                }

                // If due date has not arrived yet, keep as is
                if (dueAt.Value > now) continue;

                // 4. Overdue logic with clear precedence: Grace Period vs Suspension
                var graceDays = Math.Max(0, _options.GracePeriodDays);
                var suspendDays = Math.Max(graceDays, _options.SuspendAfterDays);
                var graceEndsAt = dueAt.Value.AddDays(graceDays);
                var suspendAt = dueAt.Value.AddDays(suspendDays);

                if (now <= graceEndsAt)
                {
                    if (subscription.Status != SubscriptionStatus.PastDue && subscription.Status != SubscriptionStatus.Suspended)
                    {
                        subscription.Status = SubscriptionStatus.PastDue;
                        subscription.PastDueSince ??= dueAt.Value;
                        await RegisterEventAsync(subscription, "billing.overdue", $"Cobrança em atraso desde {dueAt.Value:dd/MM/yyyy}; carência de {graceDays} dias em vigor.", ct);
                        processed++;
                    }
                }
                else if (now > suspendAt)
                {
                    if (subscription.Status != SubscriptionStatus.Suspended)
                    {
                        subscription.Status = SubscriptionStatus.Suspended;
                        subscription.PastDueSince ??= dueAt.Value;
                        subscription.SuspendedAt ??= now;
                        await RegisterEventAsync(subscription, "billing.suspended", $"Prazo de regularização excedido ({suspendDays} dias); benefícios suspensos.", ct);
                        processed++;
                    }
                }
                else
                {
                    if (subscription.Status != SubscriptionStatus.PastDue && subscription.Status != SubscriptionStatus.Suspended)
                    {
                        subscription.Status = SubscriptionStatus.PastDue;
                        subscription.PastDueSince ??= dueAt.Value;
                        await RegisterEventAsync(subscription, "billing.grace_ended", "Carência expirada; aguardando regularização ou suspensão.", ct);
                        processed++;
                    }
                }
            }

            page++;
        }

        if (processed > 0)
            await _uow.SaveChangesAsync(ct);

        return processed;
    }

    public Task<int> SuspendPastDueBenefitsAsync(CancellationToken ct = default)
        => SyncOverdueSubscriptionsAsync(ct);

    public async Task<int> RestorePaidBenefitsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var subscriptions = _subscriptions.Query()
            .Where(x => x.AccountId.HasValue && x.AccountId != Guid.Empty &&
                (x.Status == SubscriptionStatus.PastDue || x.Status == SubscriptionStatus.Suspended))
            .ToList();

        var restored = 0;
        foreach (var subscription in subscriptions)
        {
            var dueAt = subscription.PaidThroughAt ?? subscription.NextDueAt ?? subscription.ExpiresAt;
            if (!dueAt.HasValue) continue;

            if (!await HasProvenSettlementForCycleAsync(subscription, dueAt.Value, now, ct))
                continue;

            RestoreSubscription(subscription);
            await RegisterEventAsync(subscription, "billing.restored", "Pagamento comprovado; assinatura reativada.", ct);
            restored++;
        }

        if (restored > 0)
            await _uow.SaveChangesAsync(ct);

        return restored;
    }

    private async Task<bool> HasProvenSettlementForCycleAsync(Subscription subscription, DateTime dueAt, DateTime now, CancellationToken ct)
    {
        if (!subscription.AccountId.HasValue) return false;
        var accountId = subscription.AccountId.Value;

        var isAnnual = subscription.BillingCycle != null &&
            subscription.BillingCycle.Equals("annual", StringComparison.OrdinalIgnoreCase);
        var cycleMonths = isAnnual ? 12 : 1;

        // Check if an invoice for this cycle is fully paid
        var paidInvoice = _invoices.Query().FirstOrDefault(x =>
            x.AccountId == accountId &&
            x.SubscriptionId == subscription.Id &&
            x.Status == BillingInvoiceStatus.Paid &&
            ((x.PaidAt.HasValue && (subscription.LastPaymentAt == null || x.PaidAt.Value > subscription.LastPaymentAt.Value)) ||
             (x.DueAt >= dueAt.AddMonths(-cycleMonths))));

        if (paidInvoice != null && (subscription.LastPaymentAt == null || (paidInvoice.PaidAt.HasValue && paidInvoice.PaidAt.Value > subscription.LastPaymentAt.Value)))
        {
            var currentCoverage = subscription.PaidThroughAt ?? dueAt;
            var baseDate = currentCoverage > now ? currentCoverage : (paidInvoice.PaidAt ?? now);
            var nextCoverage = baseDate.AddMonths(cycleMonths);

            if (subscription.PaidThroughAt == null || subscription.PaidThroughAt.Value < nextCoverage)
            {
                subscription.PaidThroughAt = nextCoverage;
                subscription.NextDueAt = nextCoverage;
                subscription.LastPaymentAt = paidInvoice.PaidAt ?? now;
                subscription.Touch();
            }
            return true;
        }

        // Check if an approved payment specifically covers this obligation
        var approvedPayment = _payments.Query().FirstOrDefault(x =>
            x.AccountId == accountId &&
            x.SubscriptionId == subscription.Id &&
            x.Status == PaymentStatus.Approved &&
            x.PaidAt.HasValue &&
            ((subscription.LastPaymentAt == null || x.PaidAt.Value > subscription.LastPaymentAt.Value) ||
             (x.PaidAt.Value >= dueAt.AddMonths(-cycleMonths))));

        if (approvedPayment != null && (subscription.LastPaymentAt == null || (approvedPayment.PaidAt.HasValue && approvedPayment.PaidAt.Value > subscription.LastPaymentAt.Value)))
        {
            var currentCoverage = subscription.PaidThroughAt ?? dueAt;
            var paymentDate = approvedPayment.PaidAt ?? now;
            var baseDate = currentCoverage > now ? currentCoverage : paymentDate;
            var nextCoverage = baseDate.AddMonths(cycleMonths);

            if (subscription.PaidThroughAt == null || subscription.PaidThroughAt.Value < nextCoverage)
            {
                subscription.PaidThroughAt = nextCoverage;
                subscription.NextDueAt = nextCoverage;
                subscription.LastPaymentAt = paymentDate;
                subscription.Touch();
            }
            return true;
        }

        if (approvedPayment != null && (subscription.Status is SubscriptionStatus.PastDue or SubscriptionStatus.Suspended))
        {
            return true;
        }

        if (paidInvoice != null && (subscription.Status is SubscriptionStatus.PastDue or SubscriptionStatus.Suspended))
        {
            return true;
        }

        return false;
    }

    private static void RestoreSubscription(Subscription subscription)
    {
        subscription.Status = SubscriptionStatus.Active;
        subscription.PastDueSince = null;
        subscription.SuspendedAt = null;
        subscription.Touch();
    }

    private async Task RegisterEventAsync(Subscription subscription, string eventType, string details, CancellationToken ct)
    {
        if (!subscription.AccountId.HasValue) return;

        await _events.AddAsync(new SubscriptionEvent
        {
            AccountId = subscription.AccountId.Value,
            SubscriptionId = subscription.Id,
            EventType = eventType,
            Details = details
        }, ct);
    }
}

public class BillingOptions
{
    public int GracePeriodDays { get; set; } = 3;
    public int SuspendAfterDays { get; set; } = 5;
    public int SyncIntervalMinutes { get; set; } = 60;
}
