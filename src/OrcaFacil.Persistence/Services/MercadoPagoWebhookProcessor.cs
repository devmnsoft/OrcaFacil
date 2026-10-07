using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Billing;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

public sealed class MercadoPagoWebhookProcessor(
    OrcaFacilDbContext db,
    IPaymentGateway gateway,
    ILogger<MercadoPagoWebhookProcessor> logger) : IMercadoPagoWebhookProcessor
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
            webhookEvent.Processed = true;
            await db.SaveChangesAsync(ct);
            return new WebhookProcessingResult(true, false, validation.EventKey, "Notificação sem ID de recurso processada.", "no_resource");
        }

        // Consult the authenticated resource from the provider
        var status = await gateway.GetPaymentStatusAsync(externalId, ct);
        var paymentStatus = status.Status?.ToLowerInvariant() ?? "pending";
        decimal? amount = null;
        string? currency = "BRL";
        string? externalRef = null;
        DateTime? approvedAt = null;

        if (!string.IsNullOrWhiteSpace(status.RawResponseJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(status.RawResponseJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("transaction_amount", out var amtEl) && amtEl.TryGetDecimal(out var parsedAmt))
                    amount = parsedAmt;
                if (root.TryGetProperty("currency_id", out var curEl))
                    currency = curEl.GetString();
                if (root.TryGetProperty("external_reference", out var refEl))
                    externalRef = refEl.GetString();
                if (root.TryGetProperty("date_approved", out var dateEl) && dateEl.TryGetDateTime(out var parsedDate))
                    approvedAt = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
            catch (JsonException)
            {
                // Fallback gracefully to default parameters
            }
        }

        // Apply business effects atomically in database transaction
        using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var payment = await db.Payments
                .FirstOrDefaultAsync(x => x.ExternalPaymentId == externalId ||
                    (externalRef != null && x.ExternalReference == externalRef), ct);

            BillingInvoice? invoice = null;
            if (payment?.BillingInvoiceId is Guid invoiceId)
            {
                invoice = await db.BillingInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId, ct);
            }
            else if (!string.IsNullOrWhiteSpace(externalRef))
            {
                if (Guid.TryParse(externalRef, out var refGuid))
                {
                    invoice = await db.BillingInvoices.SingleOrDefaultAsync(x => x.Id == refGuid, ct);
                }
                else if (externalRef.StartsWith("inv:", StringComparison.OrdinalIgnoreCase) &&
                         Guid.TryParse(externalRef[4..], out var invGuid))
                {
                    invoice = await db.BillingInvoices.SingleOrDefaultAsync(x => x.Id == invGuid, ct);
                }
                else
                {
                    invoice = await db.BillingInvoices.FirstOrDefaultAsync(x => x.ExternalReference == externalRef, ct);
                }
            }

            Subscription? subscription = null;
            if (invoice != null)
            {
                subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == invoice.SubscriptionId, ct);
            }
            else if (payment?.SubscriptionId is Guid subId)
            {
                subscription = await db.Subscriptions.SingleOrDefaultAsync(x => x.Id == subId, ct);
            }
            else if (payment?.AccountId is Guid accId)
            {
                subscription = await db.Subscriptions.FirstOrDefaultAsync(x => x.AccountId == accId && !x.IsDeleted, ct);
            }

            var resolvedAccountId = subscription?.AccountId ?? invoice?.AccountId ?? payment?.AccountId;

            if (payment is null && resolvedAccountId.HasValue)
            {
                payment = new Payment
                {
                    AccountId = resolvedAccountId.Value,
                    SubscriptionId = subscription?.Id,
                    BillingInvoiceId = invoice?.Id,
                    Provider = "MercadoPago",
                    Status = PaymentStatus.Pending,
                    Amount = amount ?? invoice?.Amount ?? 0m,
                    Currency = currency ?? "BRL",
                    ExternalPaymentId = externalId,
                    ExternalReference = externalRef,
                    RawResponseJson = status.RawResponseJson
                };
                db.Payments.Add(payment);
            }

            if (paymentStatus is "approved" or "authorized" or "active")
            {
                var paidTime = approvedAt ?? DateTime.UtcNow;
                var effectiveAmount = amount ?? payment?.Amount ?? invoice?.Amount ?? 0m;

                if (payment != null)
                {
                    payment.Status = PaymentStatus.Approved;
                    payment.PaidAt = paidTime;
                    payment.ApprovedAt = paidTime;
                    payment.Amount = effectiveAmount;
                    payment.RawResponseJson = status.RawResponseJson;
                    payment.Touch();
                }

                if (invoice != null && invoice.Status != BillingInvoiceStatus.Paid)
                {
                    var settleAmount = Math.Min(effectiveAmount, invoice.Amount - invoice.PaidAmount);
                    if (settleAmount > 0)
                    {
                        invoice.ApplyPayment(settleAmount, paidTime);
                    }
                }

                if (subscription != null)
                {
                    subscription.Status = SubscriptionStatus.Active;
                    subscription.PastDueSince = null;
                    subscription.SuspendedAt = null;
                    subscription.LastPaymentAt = paidTime;

                    var isAnnual = subscription.BillingCycle != null &&
                        subscription.BillingCycle.Equals("annual", StringComparison.OrdinalIgnoreCase);
                    var cycleMonths = isAnnual ? 12 : 1;

                    var currentCoverage = subscription.PaidThroughAt;
                    var baseCoverage = (currentCoverage.HasValue && currentCoverage.Value > paidTime)
                        ? currentCoverage.Value
                        : paidTime;

                    var nextCoverage = baseCoverage.AddMonths(cycleMonths);
                    subscription.PaidThroughAt = nextCoverage;
                    subscription.NextDueAt = nextCoverage;

                    if (subscription.EffectivePlanVersionId == null && subscription.SelectedPlanVersionId.HasValue)
                    {
                        subscription.EffectivePlanVersionId = subscription.SelectedPlanVersionId;
                    }
                    subscription.Touch();

                    db.SubscriptionEvents.Add(new SubscriptionEvent
                    {
                        AccountId = subscription.AccountId ?? resolvedAccountId ?? Guid.Empty,
                        SubscriptionId = subscription.Id,
                        EventType = "billing.payment_approved",
                        Details = $"Pagamento {externalId} aprovado no valor de {effectiveAmount:C2}."
                    });
                }

                if (resolvedAccountId.HasValue)
                {
                    var account = await db.BusinessAccounts.SingleOrDefaultAsync(x => x.Id == resolvedAccountId.Value, ct);
                    if (account != null && account.Status != AccountStatus.Blocked)
                    {
                        account.FinancialStatus = "Current";
                        account.Touch();
                    }
                }
            }
            else if (paymentStatus is "refunded" or "charged_back")
            {
                if (payment != null)
                {
                    payment.Status = paymentStatus == "refunded" ? PaymentStatus.Refunded : PaymentStatus.Chargeback;
                    payment.Touch();
                }

                if (invoice != null && invoice.PaidAmount > 0)
                {
                    var reverseAmount = amount ?? invoice.PaidAmount;
                    invoice.ReversePayment(Math.Min(reverseAmount, invoice.PaidAmount));
                }

                if (subscription != null)
                {
                    subscription.Status = SubscriptionStatus.PastDue;
                    subscription.PastDueSince = DateTime.UtcNow;
                    subscription.Touch();

                    db.SubscriptionEvents.Add(new SubscriptionEvent
                    {
                        AccountId = subscription.AccountId ?? resolvedAccountId ?? Guid.Empty,
                        SubscriptionId = subscription.Id,
                        EventType = "billing.payment_refunded",
                        Details = $"Pagamento {externalId} estornado/reembolsado ({paymentStatus})."
                    });
                }
            }

            webhookEvent.Processed = true;
            webhookEvent.Touch();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogInformation("MERCADOPAGO_WEBHOOK_PROCESSED_SUCCESSFULLY EventKey {EventKey} Status {Status}",
                validation.EventKey, paymentStatus);

            return new WebhookProcessingResult(true, false, validation.EventKey, "Efeitos de pagamento aplicados com sucesso.", paymentStatus);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "MERCADOPAGO_WEBHOOK_PROCESSING_FAILED EventKey {EventKey}", validation.EventKey);
            throw;
        }
    }
}
