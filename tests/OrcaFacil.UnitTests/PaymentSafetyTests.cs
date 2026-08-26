using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Application.Payments;
using OrcaFacil.Application.Security;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class PaymentSafetyTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly Guid Invoice = Guid.NewGuid();

    [Fact]
    public async Task Checkout_without_real_provider_is_pending_and_offline()
    {
        var service = new PaymentCheckoutService(new NoopPaymentProvider());
        var result = await service.CreateAsync(new(Account, null, "invoice", 120, "brl", TimeSpan.FromMinutes(30)));
        Assert.False(result.OnlineEnabled);
        Assert.Equal(CheckoutStatus.Pending, result.Status);
        Assert.Null(result.ProviderSessionId);
    }

    [Fact]
    public async Task Pix_and_bank_slip_are_never_generated_by_unconfigured_provider()
    {
        var provider = new NoopPaymentProvider();
        var request = new PaymentCreationRequest(Account, Invoice, 50, "BRL", "invoice:1");
        Assert.False((await new PaymentPixService(provider).CreateAsync(request)).Succeeded);
        Assert.False((await new PaymentBankSlipService(provider).CreateAsync(request)).Succeeded);
    }

    [Fact]
    public void Webhook_signature_is_hmac_validated_in_constant_time()
    {
        const string payload = "{\"type\":\"payment.paid\"}";
        const string secret = "test-only-secret";
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)));
        var verifier = new HmacPaymentWebhookVerifier();
        Assert.True(verifier.Verify(payload, signature, secret).IsValid);
        Assert.False(verifier.Verify(payload, "00", secret).IsValid);
    }

    [Fact]
    public async Task Duplicate_webhook_is_accepted_without_second_processing()
    {
        const string payload = "{\"type\":\"payment.paid\",\"token\":\"sensitive\"}";
        const string secret = "test-only-secret";
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload)));
        var store = new MemoryEventStore();
        var service = new PaymentWebhookService(new HmacPaymentWebhookVerifier(), store, new SensitiveDataSanitizer());
        var envelope = new PaymentWebhookEnvelope("provider", "evt-1", "payment.paid", Account, payload, signature, DateTimeOffset.UtcNow);
        Assert.False((await service.ProcessAsync(envelope, secret)).Duplicate);
        Assert.True((await service.ProcessAsync(envelope, secret)).Duplicate);
        Assert.Equal(1, store.Completed);
        Assert.DoesNotContain("sensitive", store.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Manual_confirmation_requires_permission_and_meaningful_reason()
    {
        var service = new ManualPaymentConfirmationService(new MemoryEventStore());
        var denied = new ManualConfirmationRequest(Account, Invoice, Guid.NewGuid(), 20, DateTimeOffset.UtcNow, "Pagamento conferido", "proof-1", false, false);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmAsync(denied));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ConfirmAsync(denied with { HasPermission = true, Reason = "curto" }));
    }

    [Fact]
    public void Reconciliation_does_not_approve_divergence_implicitly()
    {
        var item = new ReconciliationCandidate(Account, "external-1", 100, 90, DateTimeOffset.UtcNow);
        var result = new PaymentReconciliationService().Reconcile(item, false, null);
        Assert.False(result.Reconciled);
        Assert.True(result.HasDivergence);
    }

    private sealed class MemoryEventStore : IPaymentEventStore
    {
        private readonly HashSet<string> _events = [];
        public int Completed { get; private set; }
        public string Payload { get; private set; } = "";
        public Task<bool> TryBeginWebhookAsync(Guid accountId, string provider, string eventId, string eventType, string sanitizedPayload, DateTimeOffset receivedAt, CancellationToken ct)
        { Payload = sanitizedPayload; return Task.FromResult(_events.Add($"{accountId}:{provider}:{eventId}")); }
        public Task CompleteWebhookAsync(Guid accountId, string provider, string eventId, string outcome, bool requiresReview, CancellationToken ct) { Completed++; return Task.CompletedTask; }
        public Task RecordAuditAsync(Guid accountId, Guid actorId, string eventType, string entityId, string detail, CancellationToken ct) => Task.CompletedTask;
    }
}
