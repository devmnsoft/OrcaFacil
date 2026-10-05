using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
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

    public BillingStatusService(
        IRepository<Subscription> subscriptions,
        IRepository<Payment> payments,
        IRepository<BillingInvoice> invoices,
        IRepository<SubscriptionEvent> events,
        IUnitOfWork uow,
        IOptions<BillingOptions>? options = null)
    {
        _subscriptions = subscriptions;
        _payments = payments;
        _invoices = invoices;
        _events = events;
        _uow = uow;
        _options = options?.Value ?? new BillingOptions();
    }

    public async Task<int> SyncOverdueSubscriptionsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var subscriptions = _subscriptions.Query()
            .Where(x => x.AccountId.HasValue && x.AccountId != Guid.Empty &&
                x.Status != SubscriptionStatus.Free &&
                x.Status != SubscriptionStatus.Cancelled &&
                x.Status != SubscriptionStatus.Canceled &&
                x.Status != SubscriptionStatus.Expired &&
                x.Status != SubscriptionStatus.ManualRelease &&
                (x.NextDueAt.HasValue || x.PaidThroughAt.HasValue || x.ExpiresAt.HasValue))
            .ToList();

        var processed = 0;
        foreach (var subscription in subscriptions)
        {
            var dueAt = subscription.PaidThroughAt ?? subscription.NextDueAt ?? subscription.ExpiresAt;
            if (!dueAt.HasValue || dueAt.Value > now)
                continue;

            if (await HasApprovedPaymentAfterDueAsync(subscription, dueAt.Value, ct))
            {
                if (subscription.Status == SubscriptionStatus.PastDue || subscription.Status == SubscriptionStatus.Suspended)
                {
                    RestoreSubscription(subscription);
                    RegisterEvent(subscription, "billing.restored", "Pagamento confirmado; benefícios restaurados.");
                    processed++;
                }
                continue;
            }

            var graceEndsAt = dueAt.Value.AddDays(Math.Max(0, _options.GracePeriodDays));
            if (subscription.Status != SubscriptionStatus.PastDue && subscription.Status != SubscriptionStatus.Suspended)
            {
                if (now <= graceEndsAt)
                {
                    subscription.Status = SubscriptionStatus.PastDue;
                    subscription.PastDueSince ??= dueAt.Value;
                    RegisterEvent(subscription, "billing.overdue", "Cobrança em atraso; carência em vigor.");
                    processed++;
                }
                else
                {
                    subscription.Status = SubscriptionStatus.Suspended;
                    subscription.PastDueSince ??= dueAt.Value;
                    subscription.SuspendedAt ??= now;
                    RegisterEvent(subscription, "billing.suspended", "Cobrança vencida e benefícios suspensos.");
                    processed++;
                }
            }
            else if (subscription.Status == SubscriptionStatus.PastDue && now > graceEndsAt)
            {
                subscription.Status = SubscriptionStatus.Suspended;
                subscription.SuspendedAt ??= now;
                RegisterEvent(subscription, "billing.suspended", "Carência excedida; benefícios suspensos.");
                processed++;
            }
        }

        if (processed > 0)
            await _uow.SaveChangesAsync(ct);

        return processed;
    }

    public async Task<int> SuspendPastDueBenefitsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var subscriptions = _subscriptions.Query()
            .Where(x => x.AccountId.HasValue && x.AccountId != Guid.Empty &&
                (x.Status == SubscriptionStatus.Active ||
                 x.Status == SubscriptionStatus.Trial ||
                 x.Status == SubscriptionStatus.Trialing ||
                 x.Status == SubscriptionStatus.PendingPayment ||
                 x.Status == SubscriptionStatus.PendingActivation ||
                 x.Status == SubscriptionStatus.PastDue))
            .ToList();

        var suspended = 0;
        foreach (var subscription in subscriptions)
        {
            var dueAt = subscription.PaidThroughAt ?? subscription.NextDueAt ?? subscription.ExpiresAt;
            if (!dueAt.HasValue || dueAt.Value > now)
                continue;

            if (await HasApprovedPaymentAfterDueAsync(subscription, dueAt.Value, ct))
                continue;

            if (now > dueAt.Value.AddDays(Math.Max(0, _options.GracePeriodDays)))
            {
                subscription.Status = SubscriptionStatus.Suspended;
                subscription.PastDueSince ??= dueAt.Value;
                subscription.SuspendedAt ??= now;
                RegisterEvent(subscription, "billing.suspended", "Cobrança vencida e benefícios bloqueados.");
                suspended++;
            }
            else if (subscription.Status != SubscriptionStatus.PastDue)
            {
                subscription.Status = SubscriptionStatus.PastDue;
                subscription.PastDueSince ??= dueAt.Value;
                RegisterEvent(subscription, "billing.overdue", "Cobrança vencida; aguardando regularização.");
                suspended++;
            }
        }

        if (suspended > 0)
            await _uow.SaveChangesAsync(ct);

        return suspended;
    }

    public async Task<int> RestorePaidBenefitsAsync(CancellationToken ct = default)
    {
        var subscriptions = _subscriptions.Query()
            .Where(x => x.AccountId.HasValue && x.AccountId != Guid.Empty &&
                (x.Status == SubscriptionStatus.PastDue || x.Status == SubscriptionStatus.Suspended))
            .ToList();

        var restored = 0;
        foreach (var subscription in subscriptions)
        {
            var dueAt = subscription.PaidThroughAt ?? subscription.NextDueAt ?? subscription.ExpiresAt;
            if (!dueAt.HasValue)
                continue;

            if (!await HasApprovedPaymentAfterDueAsync(subscription, dueAt.Value, ct))
                continue;

            RestoreSubscription(subscription);
            RegisterEvent(subscription, "billing.restored", "Pagamento confirmado; assinatura reativada.");
            restored++;
        }

        if (restored > 0)
            await _uow.SaveChangesAsync(ct);

        return restored;
    }

    private Task<bool> HasApprovedPaymentAfterDueAsync(Subscription subscription, DateTime dueAt, CancellationToken ct)
    {
        if (!subscription.AccountId.HasValue)
            return Task.FromResult(false);

        var accountId = subscription.AccountId.Value;
        var approvedPayment = _payments.Query().Any(x =>
            x.AccountId == accountId &&
            x.SubscriptionId == subscription.Id &&
            x.Status == PaymentStatus.Approved &&
            x.PaidAt >= dueAt);

        if (approvedPayment)
            return Task.FromResult(true);

        return Task.FromResult(_invoices.Query().Any(x =>
            x.AccountId == accountId &&
            x.SubscriptionId == subscription.Id &&
            x.Status == BillingInvoiceStatus.Paid &&
            x.PaidAt >= dueAt));
    }

    private static void RestoreSubscription(Subscription subscription)
    {
        subscription.Status = SubscriptionStatus.Active;
        subscription.PastDueSince = null;
        subscription.SuspendedAt = null;
    }

    private void RegisterEvent(Subscription subscription, string eventType, string details)
    {
        if (!subscription.AccountId.HasValue)
            return;

        _ = _events.AddAsync(new SubscriptionEvent
        {
            AccountId = subscription.AccountId.Value,
            SubscriptionId = subscription.Id,
            EventType = eventType,
            Details = details
        }, CancellationToken.None);
    }
}

public class BillingOptions { public int GracePeriodDays { get; set; } = 3; public int SuspendAfterDays { get; set; } = 5; }
