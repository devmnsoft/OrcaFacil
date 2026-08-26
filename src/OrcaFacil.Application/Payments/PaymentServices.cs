using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Application.Security;

namespace OrcaFacil.Application.Payments;

public enum PaymentProviderStatus { NotConfigured, Configured, Healthy, Degraded, Failed, Disabled }
public enum PaymentOrigin { Gateway, Manual }
public enum CheckoutStatus { Pending, Completed, Expired, Canceled }
public enum PaymentInstrumentStatus { Generated, Pending, Paid, Expired, Canceled, Failed, ManualConfirmed }

public sealed record PaymentProviderContext(string Name, string Environment, PaymentProviderStatus Status, bool IsRealIntegration);
public sealed record CheckoutRequest(Guid AccountId, Guid? CustomerId, string Purpose, decimal Amount, string Currency, TimeSpan Lifetime);
public sealed record CheckoutSession(Guid Id, Guid AccountId, Guid? CustomerId, string Purpose, decimal Amount, string Currency,
    DateTimeOffset ExpiresAt, CheckoutStatus Status, bool OnlineEnabled, string? ProviderSessionId = null);
public sealed record PaymentCreationRequest(Guid AccountId, Guid InvoiceId, decimal Amount, string Currency, string IdempotencyKey);
public sealed record PaymentInstrumentResult(bool Succeeded, PaymentInstrumentStatus Status, PaymentOrigin Origin,
    string? ExternalId = null, string? Payload = null, string? DisplayUrl = null, string? ErrorCode = null);
public sealed record CardTokenRequest(Guid AccountId, Guid InvoiceId, decimal Amount, string Token, string Brand, string LastFour);
public sealed record CardAuthorizationResult(bool Succeeded, string Status, string Brand, string LastFour, string? ProviderAuthorizationId, string? ErrorCode);
public sealed record WebhookVerificationResult(bool IsValid, string? ErrorCode);
public sealed record PaymentWebhookEnvelope(string Provider, string EventId, string EventType, Guid AccountId, string Payload, string Signature, DateTimeOffset ReceivedAt);
public sealed record PaymentWebhookResult(bool Accepted, bool Duplicate, bool RequiresReview, string StatusCode);
public sealed record ManualConfirmationRequest(Guid AccountId, Guid InvoiceId, Guid ActorUserId, decimal Amount,
    DateTimeOffset PaidAt, string Reason, string? EvidenceReference, bool HasPermission, bool DivergenceApproved);
public sealed record ReconciliationCandidate(Guid AccountId, string ExternalReference, decimal ExpectedAmount, decimal SettledAmount, DateTimeOffset SettledAt);
public sealed record ReconciliationResult(bool Reconciled, bool HasDivergence, string Status, decimal Difference);

public interface IPaymentProvider { PaymentProviderContext Context { get; } }
public interface IPaymentCheckoutProvider : IPaymentProvider { Task<string?> CreateSessionAsync(CheckoutRequest request, CancellationToken ct = default); }
public interface IPaymentWebhookVerifier { WebhookVerificationResult Verify(string payload, string signature, string secret); }
public interface IPaymentReconciliationProvider : IPaymentProvider { Task<IReadOnlyList<ReconciliationCandidate>> FetchAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default); }
public interface IPixPaymentProvider : IPaymentProvider { Task<PaymentInstrumentResult> CreatePixAsync(PaymentCreationRequest request, CancellationToken ct = default); }
public interface IBankSlipPaymentProvider : IPaymentProvider { Task<PaymentInstrumentResult> CreateBankSlipAsync(PaymentCreationRequest request, CancellationToken ct = default); }
public interface ICardPaymentProvider : IPaymentProvider { Task<CardAuthorizationResult> AuthorizeAsync(CardTokenRequest request, CancellationToken ct = default); }

public interface IPaymentEventStore
{
    Task<bool> TryBeginWebhookAsync(Guid accountId, string provider, string eventId, string eventType, string sanitizedPayload, DateTimeOffset receivedAt, CancellationToken ct);
    Task CompleteWebhookAsync(Guid accountId, string provider, string eventId, string outcome, bool requiresReview, CancellationToken ct);
    Task RecordAuditAsync(Guid accountId, Guid actorId, string eventType, string entityId, string detail, CancellationToken ct);
}

public sealed class NoopPaymentProvider : IPaymentCheckoutProvider, IPixPaymentProvider, IBankSlipPaymentProvider, ICardPaymentProvider, IPaymentReconciliationProvider
{
    public PaymentProviderContext Context { get; } = new("none", "none", PaymentProviderStatus.NotConfigured, false);
    public Task<string?> CreateSessionAsync(CheckoutRequest request, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<PaymentInstrumentResult> CreatePixAsync(PaymentCreationRequest request, CancellationToken ct = default) => Disabled();
    public Task<PaymentInstrumentResult> CreateBankSlipAsync(PaymentCreationRequest request, CancellationToken ct = default) => Disabled();
    public Task<CardAuthorizationResult> AuthorizeAsync(CardTokenRequest request, CancellationToken ct = default) =>
        Task.FromResult(new CardAuthorizationResult(false, "NotConfigured", Mask.Brand(CardTokenRequestBrand(request)), Mask.LastFour(request.LastFour), null, "provider_not_configured"));
    public Task<IReadOnlyList<ReconciliationCandidate>> FetchAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ReconciliationCandidate>>([]);
    private static Task<PaymentInstrumentResult> Disabled() => Task.FromResult(new PaymentInstrumentResult(false, PaymentInstrumentStatus.Failed, PaymentOrigin.Gateway, ErrorCode: "provider_not_configured"));
    private static string CardTokenRequestBrand(CardTokenRequest request) => request.Brand;
}

public sealed class ManualPaymentProvider : IPaymentProvider
{
    public PaymentProviderContext Context { get; } = new("manual", "manual", PaymentProviderStatus.Configured, false);
}

public sealed class PaymentCheckoutService(IPaymentCheckoutProvider provider, TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    public async Task<CheckoutSession> CreateAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        if (request.AccountId == Guid.Empty || request.Amount <= 0 || request.Lifetime <= TimeSpan.Zero || request.Lifetime > TimeSpan.FromHours(24))
            throw new ArgumentException("Dados da sessão de checkout inválidos.");
        var online = provider.Context is { IsRealIntegration: true, Status: PaymentProviderStatus.Healthy or PaymentProviderStatus.Configured };
        var externalId = online ? await provider.CreateSessionAsync(request, ct) : null;
        online = online && !string.IsNullOrWhiteSpace(externalId);
        return new CheckoutSession(Guid.NewGuid(), request.AccountId, request.CustomerId, request.Purpose.Trim(), request.Amount,
            request.Currency.ToUpperInvariant(), _clock.GetUtcNow().Add(request.Lifetime), CheckoutStatus.Pending, online, externalId);
    }

    public CheckoutStatus GetStatus(CheckoutSession session) =>
        session.Status == CheckoutStatus.Pending && session.ExpiresAt <= _clock.GetUtcNow() ? CheckoutStatus.Expired : session.Status;
}

public sealed class PaymentPixService(IPixPaymentProvider provider)
{
    public Task<PaymentInstrumentResult> CreateAsync(PaymentCreationRequest request, CancellationToken ct = default) =>
        IsAvailable(provider.Context) ? provider.CreatePixAsync(Validate(request), ct) : NotConfigured();
    private static Task<PaymentInstrumentResult> NotConfigured() => Task.FromResult(new PaymentInstrumentResult(false, PaymentInstrumentStatus.Failed, PaymentOrigin.Gateway, ErrorCode: "provider_not_configured"));
    internal static bool IsAvailable(PaymentProviderContext context) => context.IsRealIntegration && context.Status is PaymentProviderStatus.Healthy or PaymentProviderStatus.Configured;
    internal static PaymentCreationRequest Validate(PaymentCreationRequest request) => request.AccountId == Guid.Empty || request.InvoiceId == Guid.Empty || request.Amount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey) ? throw new ArgumentException("Cobrança inválida.") : request;
}

public sealed class PaymentBankSlipService(IBankSlipPaymentProvider provider)
{
    public Task<PaymentInstrumentResult> CreateAsync(PaymentCreationRequest request, CancellationToken ct = default) =>
        PaymentPixService.IsAvailable(provider.Context) ? provider.CreateBankSlipAsync(PaymentPixService.Validate(request), ct) : Task.FromResult(new PaymentInstrumentResult(false, PaymentInstrumentStatus.Failed, PaymentOrigin.Gateway, ErrorCode: "provider_not_configured"));
}

public sealed class PaymentCardService(ICardPaymentProvider provider)
{
    public Task<CardAuthorizationResult> AuthorizeAsync(CardTokenRequest request, CancellationToken ct = default)
    {
        if (!PaymentPixService.IsAvailable(provider.Context)) return Task.FromResult(new CardAuthorizationResult(false, "NotConfigured", Mask.Brand(request.Brand), Mask.LastFour(request.LastFour), null, "provider_not_configured"));
        if (request.AccountId == Guid.Empty || request.InvoiceId == Guid.Empty || request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Token) || !IsLastFour(request.LastFour)) throw new ArgumentException("Tokenização de cartão inválida.");
        if (request.Token.Any(char.IsWhiteSpace) || request.Token.Length < 12) throw new ArgumentException("Use somente o token seguro retornado pelo gateway.");
        return provider.AuthorizeAsync(request with { Brand = Mask.Brand(request.Brand), LastFour = Mask.LastFour(request.LastFour) }, ct);
    }
    private static bool IsLastFour(string value) => value.Length == 4 && value.All(char.IsDigit);
}

public sealed class HmacPaymentWebhookVerifier : IPaymentWebhookVerifier
{
    public WebhookVerificationResult Verify(string payload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret)) return new(false, "invalid_signature");
        try
        {
            var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
            var supplied = Convert.FromHexString(signature);
            return CryptographicOperations.FixedTimeEquals(expected, supplied) ? new(true, null) : new(false, "invalid_signature");
        }
        catch (FormatException) { return new(false, "invalid_signature"); }
    }
}

public sealed class PaymentWebhookService(IPaymentWebhookVerifier verifier, IPaymentEventStore store, ISensitiveDataSanitizer sanitizer)
{
    private static readonly HashSet<string> KnownEvents = ["payment.created", "payment.pending", "payment.paid", "payment.failed", "payment.canceled", "payment.refunded", "invoice.paid", "invoice.overdue", "subscription.updated", "subscription.canceled", "dispute.created"];
    public async Task<PaymentWebhookResult> ProcessAsync(PaymentWebhookEnvelope webhook, string secret, CancellationToken ct = default)
    {
        if (webhook.AccountId == Guid.Empty || string.IsNullOrWhiteSpace(webhook.EventId)) return new(false, false, false, "invalid_event");
        if (!verifier.Verify(webhook.Payload, webhook.Signature, secret).IsValid) return new(false, false, false, "invalid_signature");
        var clean = sanitizer.Sanitize(webhook.Payload);
        if (!await store.TryBeginWebhookAsync(webhook.AccountId, webhook.Provider, webhook.EventId, webhook.EventType, clean, webhook.ReceivedAt, ct)) return new(true, true, false, "duplicate");
        var review = !KnownEvents.Contains(webhook.EventType);
        await store.CompleteWebhookAsync(webhook.AccountId, webhook.Provider, webhook.EventId, review ? "review" : "processed", review, ct);
        return new(true, false, review, review ? "accepted_for_review" : "processed");
    }
}

public sealed class ManualPaymentConfirmationService(IPaymentEventStore store)
{
    public async Task ConfirmAsync(ManualConfirmationRequest request, CancellationToken ct = default)
    {
        if (!request.HasPermission) throw new UnauthorizedAccessException("Permissão Payments.ManualConfirm obrigatória.");
        if (request.AccountId == Guid.Empty || request.InvoiceId == Guid.Empty || request.ActorUserId == Guid.Empty || request.Amount <= 0 || request.PaidAt > DateTimeOffset.UtcNow.AddMinutes(5)) throw new ArgumentException("Dados de confirmação inválidos.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10) throw new ArgumentException("Informe uma justificativa com pelo menos 10 caracteres.");
        if (!request.DivergenceApproved && request.Reason.Contains("diverg", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("A divergência exige aprovação adicional.");
        await store.RecordAuditAsync(request.AccountId, request.ActorUserId, "payment.manual_confirmed", request.InvoiceId.ToString(), $"amount={request.Amount:F2};paidAt={request.PaidAt:O};evidence={(string.IsNullOrWhiteSpace(request.EvidenceReference) ? "none" : "attached")}", ct);
    }
}

public sealed class PaymentReconciliationService
{
    public ReconciliationResult Reconcile(ReconciliationCandidate item, bool manuallyApproved, string? reason)
    {
        if (item.AccountId == Guid.Empty || string.IsNullOrWhiteSpace(item.ExternalReference) || item.ExpectedAmount <= 0 || item.SettledAmount <= 0) throw new ArgumentException("Linha de conciliação inválida.");
        var difference = item.SettledAmount - item.ExpectedAmount;
        if (difference != 0 && (!manuallyApproved || string.IsNullOrWhiteSpace(reason))) return new(false, true, "Divergent", difference);
        if (manuallyApproved && (reason?.Trim().Length ?? 0) < 10) throw new ArgumentException("A conciliação manual exige motivo.");
        return new(true, difference != 0, difference == 0 ? "Matched" : "ManuallyApproved", difference);
    }
}

internal static class Mask
{
    public static string LastFour(string value) => value.Length >= 4 ? value[^4..] : "****";
    public static string Brand(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : new string(value.Where(char.IsLetterOrDigit).Take(20).ToArray()).ToLowerInvariant();
}
