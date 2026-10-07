using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Billing;
using OrcaFacil.Application.Payments;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class MercadoPagoWebhookProcessor(
    OrcaFacilDbContext db,
    IPaymentGateway gateway,
    ILogger<MercadoPagoWebhookProcessor> logger,
    IOptions<MercadoPagoOptions> mercadoPagoOptions) : IMercadoPagoWebhookProcessor
{
    public async Task<WebhookProcessingResult> ProcessAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var validation = await gateway.HandleWebhookAsync(rawBody, headers, ct);
        if (!validation.Processed)
        {
            logger.LogWarning("MERCADOPAGO_WEBHOOK_VALIDATION_FAILED Status {Status}", validation.Status);
            return new WebhookProcessingResult(false, false, validation.EventKey, "Assinatura do webhook inválida ou não autorizada.", "unauthorized");
        }

        var existing = await db.MercadoPagoWebhookEvents
            .SingleOrDefaultAsync(x => x.EventKey == validation.EventKey, ct);

        if (existing is { Processed: true })
        {
            logger.LogInformation("MERCADOPAGO_WEBHOOK_DUPLICATE_IGNORED EventKey {EventKey}", validation.EventKey);
            return new WebhookProcessingResult(true, true, validation.EventKey, "Evento já processado com sucesso anteriormente.", "duplicate");
        }

        var webhookEvent = existing ?? new MercadoPagoWebhookEvent
        {
            EventKey = validation.EventKey,
            ExternalPaymentId = validation.ExternalPaymentId,
            Topic = validation.Topic,
            RawJson = rawBody,
            Processed = false,
            CorrelationId = correlationId
        };

        if (existing is null)
        {
            db.MercadoPagoWebhookEvents.Add(webhookEvent);
            await db.SaveChangesAsync(ct);
        }

        var externalId = validation.ExternalPaymentId;
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return new WebhookProcessingResult(true, false, validation.EventKey, "Notificação sem recurso consultável. Nenhum efeito financeiro foi aplicado.", "no_resource");
        }

        PaymentGatewayStatus status;
        try
        {
            status = await gateway.GetPaymentStatusAsync(externalId, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MERCADOPAGO_WEBHOOK_STATUS_UNAVAILABLE EventKey {EventKey}", validation.EventKey);
            return new WebhookProcessingResult(false, false, validation.EventKey, "A consulta do pagamento falhou e poderá ser repetida.", "transient");
        }

        var facts = BillingSettlementPolicy.Read(status.Status, status.RawResponseJson);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var payment = await db.Payments.FirstOrDefaultAsync(x => x.ExternalPaymentId == externalId && !x.IsDeleted, ct);
            var invoice = await FindInvoiceAsync(payment, facts.ExternalReference, ct);
            var subscription = await FindSubscriptionAsync(payment, invoice, ct);
            if (payment is null && (invoice is not null || subscription is not null) && facts.Amount is decimal knownAmount && !string.IsNullOrWhiteSpace(facts.Currency))
            {
                var accountId = invoice?.AccountId ?? subscription?.AccountId;
                if (accountId.HasValue)
                {
                    payment = new Payment
                    {
                        AccountId = accountId,
                        SubscriptionId = subscription?.Id ?? invoice?.SubscriptionId,
                        BillingInvoiceId = invoice?.Id,
                        Provider = "MercadoPago",
                        Status = PaymentStatus.Pending,
                        Amount = knownAmount,
                        Currency = facts.Currency,
                        ExternalPaymentId = externalId,
                        ExternalReference = facts.ExternalReference,
                        RawResponseJson = status.RawResponseJson
                    };
                    db.Payments.Add(payment);
                }
            }

            var evidence = new SettlementEvidence(
                string.IsNullOrWhiteSpace(mercadoPagoOptions.Value.Environment) ? "Sandbox" : mercadoPagoOptions.Value.Environment,
                invoice?.Amount is decimal invoiceAmount && invoiceAmount > 0m ? invoiceAmount : subscription?.Amount > 0m ? subscription.Amount : null,
                invoice?.Currency ?? (subscription is null ? null : "BRL"),
                invoice is not null,
                subscription is not null,
                await AccountIsBlockedAsync(invoice?.AccountId ?? subscription?.AccountId ?? payment?.AccountId, ct),
                payment?.Status == PaymentStatus.Approved,
                payment?.Status is PaymentStatus.Refunded or PaymentStatus.Chargeback,
                await EffectExistsAsync(externalId, BillingSettlementPolicy.EffectExtended, ct),
                await EffectExistsAsync(externalId, BillingSettlementPolicy.EffectReversed, ct),
                BillingSettlementPolicy.CycleMonths(subscription?.BillingCycle ?? invoice?.Cycle.ToString()));

            var decision = BillingSettlementPolicy.Decide(facts, evidence);
            if (!decision.CompleteEvent)
            {
                await tx.RollbackAsync(ct);
                logger.LogWarning("MERCADOPAGO_WEBHOOK_UNRESOLVED EventKey {EventKey} Code {Code}", validation.EventKey, decision.Code);
                return new WebhookProcessingResult(false, false, validation.EventKey, "O recurso ainda não foi comprovado. Nenhum benefício foi ativado.", decision.Code);
            }

            await ApplyAsync(decision, facts, payment, invoice, subscription, externalId, status.RawResponseJson, ct);
            webhookEvent.Processed = true;
            webhookEvent.Touch();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("MERCADOPAGO_WEBHOOK_PROCESSED EventKey {EventKey} Code {Code}", validation.EventKey, decision.Code);
            return new WebhookProcessingResult(true, false, validation.EventKey, "Evento registrado sem repetir cobertura.", decision.Code);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "MERCADOPAGO_WEBHOOK_PROCESSING_FAILED EventKey {EventKey}", validation.EventKey);
            throw;
        }
    }

    private async Task ApplyAsync(
        SettlementDecision decision,
        GatewayPaymentFacts facts,
        Payment? payment,
        BillingInvoice? invoice,
        Subscription? subscription,
        string externalId,
        string? rawJson,
        CancellationToken ct)
    {
        if (decision.Action == SettlementAction.ApplyFullCoverage && subscription is not null && facts.ApprovedAt is DateTime paidAt)
        {
            if (!await OwnEffectAsync(externalId, BillingSettlementPolicy.EffectExtended, subscription, invoice, payment, decision, ct))
                return;
            MarkPayment(payment, PaymentStatus.Approved, paidAt, decision.Amount, facts.Currency, rawJson);
            if (invoice is not null && invoice.Status != BillingInvoiceStatus.Paid)
                invoice.ApplyPayment(invoice.Amount - invoice.PaidAmount, paidAt);
            var next = BillingSettlementPolicy.NextCoverage(subscription.PaidThroughAt, paidAt, decision.CycleMonths);
            subscription.Status = SubscriptionStatus.Active;
            subscription.PastDueSince = null;
            subscription.SuspendedAt = null;
            subscription.LastPaymentAt = paidAt;
            subscription.PaidThroughAt = next;
            subscription.NextDueAt = next;
            if (subscription.EffectivePlanVersionId is null && subscription.SelectedPlanVersionId.HasValue)
                subscription.EffectivePlanVersionId = subscription.SelectedPlanVersionId;
            subscription.Touch();
            AddSubscriptionEvent(subscription, "billing.payment_approved", externalId);
            await SetFinancialStatusAsync(subscription.AccountId, decision.UpdateFinancialStatus, ct);
            return;
        }

        if (decision.Action == SettlementAction.RecordPartialWithoutCoverage && invoice is not null && facts.ApprovedAt is DateTime partialAt)
        {
            if (subscription is null || !await OwnEffectAsync(externalId, "partial_recorded", subscription, invoice, payment, decision, ct))
            {
                MarkPayment(payment, PaymentStatus.Pending, null, decision.Amount, facts.Currency, rawJson);
                return;
            }
            MarkPayment(payment, PaymentStatus.Pending, null, decision.Amount, facts.Currency, rawJson);
            var remaining = invoice.Amount - invoice.PaidAmount;
            if (decision.Amount > 0m && decision.Amount <= remaining && invoice.Status is not BillingInvoiceStatus.Cancelled and not BillingInvoiceStatus.Uncollectible)
                invoice.ApplyPayment(decision.Amount, partialAt);
            return;
        }

        if (decision.Action == SettlementAction.ReverseMatchedCoverage && subscription is not null)
        {
            if (!await OwnEffectAsync(externalId, BillingSettlementPolicy.EffectReversed, subscription, invoice, payment, decision, ct))
                return;
            MarkPayment(payment, facts.Status == "charged_back" ? PaymentStatus.Chargeback : PaymentStatus.Refunded, payment?.PaidAt, payment?.Amount ?? decision.Amount, facts.Currency, rawJson);
            if (invoice is not null && invoice.PaidAmount > 0m)
                invoice.ReversePayment(Math.Min(decision.Amount, invoice.PaidAmount));
            subscription.PaidThroughAt = BillingSettlementPolicy.ReverseCoverage(subscription.PaidThroughAt, decision.CycleMonths);
            subscription.NextDueAt = subscription.PaidThroughAt;
            if (subscription.PaidThroughAt is null || subscription.PaidThroughAt <= DateTime.UtcNow)
            {
                subscription.Status = SubscriptionStatus.PastDue;
                subscription.PastDueSince ??= DateTime.UtcNow;
            }
            subscription.Touch();
            AddSubscriptionEvent(subscription, "billing.payment_refunded", externalId);
            return;
        }

        if (decision.Action == SettlementAction.ObserveWithoutBenefit && payment is not null && payment.Status != PaymentStatus.Approved)
        {
            if (facts.Status is "rejected") payment.Status = PaymentStatus.Rejected;
            else if (facts.Status is "cancelled") payment.Status = PaymentStatus.Cancelled;
            payment.Touch();
        }
    }

    private async Task<bool> OwnEffectAsync(
        string externalId,
        string effect,
        Subscription subscription,
        BillingInvoice? invoice,
        Payment? payment,
        SettlementDecision decision,
        CancellationToken ct)
    {
        if (subscription.AccountId is not Guid accountId) return false;
        var rows = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO orcafacil.billing_coverage_applications
                (id, account_id, subscription_id, payment_id, invoice_id, external_payment_id, effect, cycle_months, amount, currency, created_at, is_deleted)
            VALUES (
                {Guid.NewGuid()}, {accountId}, {subscription.Id}, {payment?.Id}, {invoice?.Id}, {externalId}, {effect},
                {decision.CycleMonths}, {decision.Amount}, {invoice?.Currency ?? "BRL"}, {DateTime.UtcNow}, false)
            ON CONFLICT (external_payment_id, effect) DO NOTHING
            """, ct);
        return rows == 1;
    }

    private async Task<bool> EffectExistsAsync(string externalId, string effect, CancellationToken ct) =>
        await db.BillingCoverageApplications.AsNoTracking().AnyAsync(x => x.ExternalPaymentId == externalId && x.Effect == effect && !x.IsDeleted, ct);

    private async Task<BillingInvoice?> FindInvoiceAsync(Payment? payment, string? externalReference, CancellationToken ct)
    {
        if (payment?.BillingInvoiceId is Guid invoiceId)
            return await db.BillingInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, ct);
        if (string.IsNullOrWhiteSpace(externalReference)) return null;
        if (Guid.TryParse(externalReference, out var referenceId))
            return await db.BillingInvoices.SingleOrDefaultAsync(x => x.Id == referenceId && !x.IsDeleted, ct);
        const string prefix = "inv:";
        if (externalReference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && Guid.TryParse(externalReference[prefix.Length..], out var prefixedId))
            return await db.BillingInvoices.SingleOrDefaultAsync(x => x.Id == prefixedId && !x.IsDeleted, ct);
        return await db.BillingInvoices.SingleOrDefaultAsync(x => x.ExternalReference == externalReference && !x.IsDeleted, ct);
    }

    private async Task<Subscription?> FindSubscriptionAsync(Payment? payment, BillingInvoice? invoice, CancellationToken ct)
    {
        var subscriptionId = invoice?.SubscriptionId ?? payment?.SubscriptionId;
        if (subscriptionId is not Guid id || id == Guid.Empty) return null;
        var subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (subscription is null) return null;
        if (invoice is not null && subscription.AccountId != invoice.AccountId) return null;
        if (payment?.AccountId is Guid paymentAccount && subscription.AccountId != paymentAccount) return null;
        return subscription;
    }

    private async Task<bool> AccountIsBlockedAsync(Guid? accountId, CancellationToken ct)
    {
        if (accountId is not Guid id) return false;
        var account = await db.BusinessAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        return account?.Status == AccountStatus.Blocked;
    }

    private async Task SetFinancialStatusAsync(Guid? accountId, bool current, CancellationToken ct)
    {
        if (!current || accountId is not Guid id) return;
        var account = await db.BusinessAccounts.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (account is null || account.Status == AccountStatus.Blocked) return;
        account.FinancialStatus = "Current";
        account.Touch();
    }

    private void AddSubscriptionEvent(Subscription subscription, string eventType, string externalId)
    {
        if (subscription.AccountId is not Guid accountId) return;
        db.SubscriptionEvents.Add(new SubscriptionEvent
        {
            AccountId = accountId,
            SubscriptionId = subscription.Id,
            EventType = eventType,
            Details = $"Pagamento {externalId}: efeito único de cobertura."
        });
    }

    private static void MarkPayment(Payment? payment, PaymentStatus status, DateTime? paidAt, decimal amount, string? currency, string? rawJson)
    {
        if (payment is null) return;
        if (payment.Status is PaymentStatus.Refunded or PaymentStatus.Chargeback) return;
        if (payment.Status == PaymentStatus.Approved && status is PaymentStatus.Pending or PaymentStatus.Rejected or PaymentStatus.Cancelled) return;
        payment.Status = status;
        if (paidAt.HasValue)
        {
            payment.PaidAt = paidAt;
            payment.ApprovedAt = paidAt;
        }
        if (amount > 0m) payment.Amount = amount;
        if (!string.IsNullOrWhiteSpace(currency)) payment.Currency = currency;
        payment.RawResponseJson = rawJson;
        payment.Touch();
    }
}
