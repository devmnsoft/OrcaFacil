using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Billing;
using OrcaFacil.Application.Payments;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.Common;
using OrcaFacil.Application.Plans;
using OrcaFacil.Infrastructure.Ai;
using OrcaFacil.Infrastructure.Payments;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialCandidateHomologationTests
{
    // =========================================================================
    // 1. GATEWAY MERCADO PAGO E ASSINATURA DE WEBHOOK
    // =========================================================================

    [Fact]
    public async Task MercadoPago_HandleWebhook_validates_manifest_signature_correctly()
    {
        const string secret = "test_webhook_secret_key_12345";
        var options = Options.Create(new MercadoPagoOptions
        {
            Enabled = true,
            AccessToken = "TEST-1234567890-test-token",
            WebhookSecret = secret
        });

        var gateway = new MercadoPagoPaymentGateway(options);

        const string dataId = "123456789";
        const string requestId = "req-abc-987";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // Official Mercado Pago template: id:[data.id];request-id:[x-request-id];ts:[ts];
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var validHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();

        var body = JsonSerializer.Serialize(new { data = new { id = dataId } });
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["x-signature"] = $"ts={ts},v1={validHash}",
            ["x-request-id"] = requestId
        };

        var result = await gateway.HandleWebhookAsync(body, headers);

        Assert.True(result.Processed);
        Assert.Equal("verified", result.Status);
        Assert.Equal(dataId, result.ExternalPaymentId);
    }

    [Fact]
    public async Task MercadoPago_HandleWebhook_rejects_tampered_signature()
    {
        const string secret = "test_webhook_secret_key_12345";
        var options = Options.Create(new MercadoPagoOptions
        {
            Enabled = true,
            AccessToken = "TEST-token",
            WebhookSecret = secret
        });

        var gateway = new MercadoPagoPaymentGateway(options);

        var body = JsonSerializer.Serialize(new { data = new { id = "123456" } });
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["x-signature"] = "ts=1234567890,v1=tampered_invalid_hex_signature",
            ["x-request-id"] = "req-1"
        };

        var result = await gateway.HandleWebhookAsync(body, headers);

        Assert.False(result.Processed);
        Assert.Equal("invalid_signature", result.Status);
    }

    [Fact]
    public async Task MercadoPago_HandleWebhook_rejects_empty_signature_when_secret_is_configured()
    {
        var options = Options.Create(new MercadoPagoOptions
        {
            Enabled = true,
            AccessToken = "TEST-token",
            WebhookSecret = "mandatory-secret"
        });

        var gateway = new MercadoPagoPaymentGateway(options);
        var body = JsonSerializer.Serialize(new { data = new { id = "123" } });
        var headers = new Dictionary<string, string>();

        var result = await gateway.HandleWebhookAsync(body, headers);

        Assert.False(result.Processed);
    }

    [Theory]
    [InlineData("weekly")]
    [InlineData("quarterly")]
    [InlineData("semester")]
    [InlineData("invalid_cycle")]
    public async Task SubscriptionCheckout_rejects_unsupported_billing_cycles(string cycle)
    {
        var gateway = new MercadoPagoPaymentGateway(Options.Create(new MercadoPagoOptions()));
        var mockCatalog = new StubPlanCatalogService(new(PlanCode: "PRO", MonthlyPrice: 99m, AnnualPrice: 990m));
        var service = new SubscriptionCheckoutService(gateway, mockCatalog);

        var request = new OrcaFacil.Application.Billing.CheckoutRequest(
            AccountId: Guid.NewGuid(),
            PlanCode: "PRO",
            BillingCycle: cycle,
            PayerEmail: "cliente@teste.com",
            DocumentType: "CPF",
            DocumentNumber: "12345678909",
            IdempotencyKey: Guid.NewGuid().ToString("N"));

        var result = await service.CreateAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_billing_cycle", result.Code);
    }

    // =========================================================================
    // 2. BILLING STATUS SERVICE (PRAZOS, SUSPENSÃO E ANTECIPAÇÃO)
    // =========================================================================

    [Fact]
    public async Task Billing_prepayment_advances_coverage_from_current_paid_through()
    {
        var testClock = new TestClock(new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc));
        var options = Options.Create(new BillingOptions { GracePeriodDays = 3, SuspendAfterDays = 5 });

        var subRepo = new InMemoryRepository<Subscription>();
        var payRepo = new InMemoryRepository<Payment>();
        var invRepo = new InMemoryRepository<BillingInvoice>();
        var evtRepo = new InMemoryRepository<SubscriptionEvent>();
        var uow = new NoopUnitOfWork();

        // Subscription already paid until 2026-11-06 (1 month ahead)
        var subscription = new Subscription
        {
            AccountId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BillingCycle = "monthly",
            Status = SubscriptionStatus.Active,
            PaidThroughAt = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc),
            NextDueAt = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc)
        };
        await subRepo.AddAsync(subscription);

        var approvedPayment = new Payment
        {
            AccountId = subscription.AccountId,
            SubscriptionId = subscription.Id,
            Amount = 99m,
            Status = PaymentStatus.Approved,
            PaidAt = testClock.UtcNow
        };
        await payRepo.AddAsync(approvedPayment);

        var service = new BillingStatusService(subRepo, payRepo, invRepo, evtRepo, uow, options, testClock);
        await service.SyncOverdueSubscriptionsAsync();

        // Coverage advances from 2026-11-06 to 2026-12-06!
        Assert.Equal(new DateTime(2026, 12, 6, 12, 0, 0, DateTimeKind.Utc), subscription.PaidThroughAt);
        Assert.Equal(new DateTime(2026, 12, 6, 12, 0, 0, DateTimeKind.Utc), subscription.NextDueAt);
    }

    [Fact]
    public async Task Billing_overdue_within_grace_period_remains_past_due()
    {
        // 2 days overdue (within 3-day grace period)
        var now = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var testClock = new TestClock(now);
        var options = Options.Create(new BillingOptions { GracePeriodDays = 3, SuspendAfterDays = 5 });

        var subRepo = new InMemoryRepository<Subscription>();
        var payRepo = new InMemoryRepository<Payment>();
        var invRepo = new InMemoryRepository<BillingInvoice>();
        var evtRepo = new InMemoryRepository<SubscriptionEvent>();
        var uow = new NoopUnitOfWork();

        var subscription = new Subscription
        {
            AccountId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            NextDueAt = now.AddDays(-2),
            PaidThroughAt = now.AddDays(-2)
        };
        await subRepo.AddAsync(subscription);

        var service = new BillingStatusService(subRepo, payRepo, invRepo, evtRepo, uow, options, testClock);
        var processed = await service.SyncOverdueSubscriptionsAsync();

        Assert.Equal(1, processed);
        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
    }

    [Fact]
    public async Task Billing_overdue_beyond_grace_period_is_suspended()
    {
        // 6 days overdue (exceeds 3-day grace and 5-day suspend period)
        var now = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var testClock = new TestClock(now);
        var options = Options.Create(new BillingOptions { GracePeriodDays = 3, SuspendAfterDays = 5 });

        var subRepo = new InMemoryRepository<Subscription>();
        var payRepo = new InMemoryRepository<Payment>();
        var invRepo = new InMemoryRepository<BillingInvoice>();
        var evtRepo = new InMemoryRepository<SubscriptionEvent>();
        var uow = new NoopUnitOfWork();

        var subscription = new Subscription
        {
            AccountId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            NextDueAt = now.AddDays(-6),
            PaidThroughAt = now.AddDays(-6)
        };
        await subRepo.AddAsync(subscription);

        var service = new BillingStatusService(subRepo, payRepo, invRepo, evtRepo, uow, options, testClock);
        var processed = await service.SyncOverdueSubscriptionsAsync();

        Assert.Equal(1, processed);
        Assert.Equal(SubscriptionStatus.Suspended, subscription.Status);
    }

    // =========================================================================
    // 3. IA: CIRCUIT BREAKER, RATE LIMITING, ESCOPO E REDAÇÃO
    // =========================================================================

    [Fact]
    public void AiCircuitBreaker_opens_after_3_failures_and_blocks_attempts()
    {
        var cb = new AiCircuitBreaker();
        const string provider = "Groq";

        Assert.True(cb.CanAttempt(provider));

        cb.RecordFailure(provider);
        Assert.True(cb.CanAttempt(provider));

        cb.RecordFailure(provider);
        Assert.True(cb.CanAttempt(provider));

        cb.RecordFailure(provider);
        // Third failure trips breaker
        Assert.False(cb.CanAttempt(provider));

        // Success resets circuit
        cb.RecordSuccess(provider);
        Assert.True(cb.CanAttempt(provider));
    }

    [Fact]
    public async Task BudgetAiAssistant_is_strictly_bounded_to_tenant_catalog()
    {
        var cb = new AiCircuitBreaker();
        var orchestrator = new StubAiOrchestrator();
        var sanitizer = new PromptSanitizer(new AiRedactionService());
        var assistant = new BudgetAiAssistant(orchestrator, sanitizer);

        var accountId = Guid.NewGuid();
        var context = new AiRequestContext(accountId, Guid.NewGuid(), new HashSet<string> { "Ai.Suggest" });
        var policy = new AiGovernancePolicy(accountId);

        var catalogItem1 = new ServiceCatalogItem
        {
            AccountId = accountId,
            Name = "Troca de Disjuntor Monopolar",
            StandardPrice = 120.00m,
            UnitCode = "UN",
            IsActive = true
        };
        var catalogItem2 = new ServiceCatalogItem
        {
            AccountId = accountId,
            Name = "Instalação de Tomada 20A",
            StandardPrice = 65.00m,
            UnitCode = "UN",
            IsActive = true
        };

        var availableCatalog = new List<ServiceCatalogItem> { catalogItem1, catalogItem2 };

        var result = await assistant.SuggestBudgetAsync(
            context,
            policy,
            "Preciso de troca de disjuntor e nova tomada na cozinha",
            availableCatalog);

        Assert.True(result.Succeeded);
        Assert.NotEmpty(result.Items);
        // Prices must match catalog exactly
        Assert.All(result.Items, item => Assert.True(item.MatchedFromCatalog));
        Assert.Contains(result.Items, x => x.CatalogItemId == catalogItem1.Id && x.UnitPrice == 120.00m);
        Assert.Contains(result.Items, x => x.CatalogItemId == catalogItem2.Id && x.UnitPrice == 65.00m);
    }

    [Fact]
    public void MessageDraftAiAssistant_redacts_secrets_and_appends_disclaimers()
    {
        var assistant = new MessageDraftAiAssistant(new AiRedactionService());
        var accountId = Guid.NewGuid();
        var context = new AiRequestContext(accountId, Guid.NewGuid(), new HashSet<string> { "Ai.Draft" });
        var policy = new AiGovernancePolicy(accountId);

        var result = assistant.GenerateDraft(
            context,
            policy,
            "WhatsApp",
            "Presentation",
            "João Silva password=secret123",
            "ORC-001 Bearer sk_live_12345678901234",
            550.00m,
            new DateTime(2026, 10, 20));

        Assert.False(result.Sent);
        Assert.NotEmpty(result.Disclaimers);
        // Secrets redacted
        Assert.DoesNotContain("secret123", result.DraftContent);
        Assert.DoesNotContain("sk_live_", result.DraftContent);
        // Never sent autonomously
        Assert.Contains(result.Disclaimers, d => d.Contains("NUNCA é enviado automaticamente"));
    }

    [Fact]
    public async Task CommercialAiReviewer_detects_missing_client_items_and_zero_prices()
    {
        var reviewer = new CommercialAiReviewer();
        var accountId = Guid.NewGuid();
        var context = new AiRequestContext(accountId, Guid.NewGuid(), new HashSet<string> { "Ai.Review" });
        var policy = new AiGovernancePolicy(accountId);

        var doc = new Document
        {
            ClientName = "", // Empty
            ValidUntil = null // No validity
        };
        var items = new List<DocumentItem>
        {
            new() { Description = "", UnitPrice = 0m }
        };

        var result = await reviewer.ReviewQuoteAsync(context, policy, doc, items, null);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Findings, f => f.Category == "Cliente");
        Assert.Contains(result.Findings, f => f.Category == "Valores");
        Assert.Contains(result.Findings, f => f.Category == "Condições");
    }

    // =========================================================================
    // 4. MULTI-TENANT ISOLATION
    // =========================================================================

    [Fact]
    public async Task ContextualAiHelp_filters_out_sources_from_other_accounts()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        var orchestrator = new StubAiOrchestrator();
        var helpService = new ContextualAiHelpService(new AiPromptInjectionGuard(), orchestrator);

        var contextA = new AiRequestContext(accountA, Guid.NewGuid(), new HashSet<string> { "Ai.Help" });

        var sources = new List<AiSource>
        {
            new(accountA, "doc", "1", "Regra de Faturamento Conta A", "url1", "Texto explicativo da Conta A", true),
            new(accountB, "doc", "2", "Segredos Confidenciais Conta B", "url2", "Dados sensíveis da Conta B", true)
        };

        var answer = await helpService.AskHelpAsync(contextA, "Faturamento", sources);

        // Result must NOT contain or reference Account B sources!
        Assert.DoesNotContain(answer.Sources, s => s.AccountId == accountB);
        Assert.DoesNotContain("Conta B", answer.Text);
    }
}

// =============================================================================
// TEST DOUBLES / STUBS
// =============================================================================

internal sealed class TestClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}

internal sealed class StubPlanCatalogService(StubPlanItem plan) : IPlanCatalogService
{
    public Task<PlanCatalogView> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        var item = new PlanCardView(
            plan.PlanCode,
            plan.PlanCode,
            "Descrição",
            plan.MonthlyPrice,
            plan.AnnualPrice,
            "BRL",
            true,
            1,
            [],
            []);
        return Task.FromResult(new PlanCatalogView([item], DateTime.UtcNow, false));
    }
}

internal sealed record StubPlanItem(string PlanCode, decimal MonthlyPrice, decimal AnnualPrice);

internal sealed class StubAiOrchestrator : IAiOrchestrator
{
    public Task<AiExecutionResult> ExecuteAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string purpose,
        AiClientRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(new AiExecutionResult(
            Succeeded: true,
            Content: "Sugestão baseada em regras gerada para teste.",
            Mode: AiOperatingMode.RulesOnly,
            Provider: "RulesFallback",
            Model: "DeterministicRules",
            PromptTokens: 10,
            CompletionTokens: 10,
            LatencyMs: 5,
            IsFallbackToRules: true));
    }
}

internal sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items = [];

    public Task<T?> GetAsync(Guid id, CancellationToken ct = default)
    {
        if (typeof(Entity).IsAssignableFrom(typeof(T)))
        {
            var match = _items.Cast<Entity>().FirstOrDefault(x => x.Id == id && !x.IsDeleted);
            return Task.FromResult(match as T);
        }
        return Task.FromResult<T?>(null);
    }

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public void Remove(T entity)
    {
        _items.Remove(entity);
    }

    public IQueryable<T> Query() => _items.AsQueryable();
}

internal sealed class NoopUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task CommitTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
}
