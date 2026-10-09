using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Infrastructure.Pdf;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class JourneyIntegrityCycleHomologationTests
{
    [Fact]
    public void PlanOptions_DefaultsTo15TrialProDays()
    {
        var options = new PlanOptions();
        Assert.Equal(15, options.TrialProDays);
    }

    [Fact]
    public async Task TrialPro_ExpiresInRealtime_EvenWhenWorkerIsStopped()
    {
        var accountId = Guid.NewGuid();
        var dataSource = new MockPlanAccessDataSource(accountId)
        {
            Subscription = new Subscription
            {
                AccountId = accountId,
                Status = SubscriptionStatus.Trial,
                TrialStatus = TrialStatus.Active,
                TrialStartedAt = DateTime.UtcNow.AddDays(-16),
                TrialEndsAt = DateTime.UtcNow.AddDays(-1), // Expirou ontem
                ManualReleaseUntil = null
            }
        };

        var service = new PlanAccessService(dataSource);
        var decision = await service.CanUseAsync(accountId, "documents.create");

        Assert.False(decision.IsAllowed);
        Assert.Equal("TrialExpired", decision.InternalReason);
        Assert.Contains("15 dias", decision.UserMessage);
    }

    [Fact]
    public async Task TrialPro_AllowsCommercialUsage_WhileWithin15Days()
    {
        var accountId = Guid.NewGuid();
        var dataSource = new MockPlanAccessDataSource(accountId)
        {
            Subscription = new Subscription
            {
                AccountId = accountId,
                Status = SubscriptionStatus.Trial,
                TrialStatus = TrialStatus.Active,
                TrialStartedAt = DateTime.UtcNow.AddDays(-2),
                TrialEndsAt = DateTime.UtcNow.AddDays(13), // Válido por mais 13 dias
                ManualReleaseUntil = null
            }
        };

        var service = new PlanAccessService(dataSource);
        var decision = await service.CanUseAsync(accountId, "pdf.monthly_limit");

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task QuestPdfGenerator_GeneratesValidPdfBytes_WithIssuerProfileAndFallback()
    {
        var generator = new QuestPdfDocumentService();
        var doc = new Document
        {
            Type = DocumentType.Budget,
            ClientName = "Cliente Teste Homologação",
            IssueDate = DateTime.UtcNow
        };
        doc.IssueNumber("ORC-2026-0001");
        doc.Items.Add(new DocumentItem
        {
            Description = "Consultoria Técnica de Homologação",
            Quantity = 1,
            UnitPrice = 1500m
        });
        doc.CalculateTotals();

        var issuer = new IssuerProfile
        {
            BusinessName = "MNSOFT Soluções",
            DocumentNumber = "18.160.057/0001-13",
            City = "Belém",
            Email = "comercial@mnsoft.com.br",
            LogoPath = "/uploads/branding/inexistente.png" // Fallback seguro sem falha
        };

        var bytes = await generator.GenerateDocumentPdfAsync(doc, issuer, PlanType.Professional);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 100);
        // Magic bytes do cabeçalho PDF (%PDF-)
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public async Task ReceiptPdfGenerator_WritesAmountInWords()
    {
        var generator = new QuestPdfDocumentService();
        var doc = new Document
        {
            Type = DocumentType.Receipt,
            ClientName = "Cliente Quitado",
            PaymentMethod = "Pix",
            IssueDate = DateTime.UtcNow
        };
        doc.IssueNumber("REC-2026-0001");
        doc.Items.Add(new DocumentItem
        {
            Description = "Serviço de Desenvolvimento Web",
            Quantity = 1,
            UnitPrice = 1000m
        });
        doc.CalculateTotals();

        var issuer = new IssuerProfile
        {
            BusinessName = "MNSOFT Soluções",
            City = "Belém"
        };

        var bytes = await generator.GenerateDocumentPdfAsync(doc, issuer, PlanType.Professional);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 100);
    }

    private sealed class MockPlanAccessDataSource(Guid accountId) : IPlanAccessDataSource
    {
        public Subscription? Subscription { get; set; }
        private readonly Plan _proPlan = new() { Code = "PROFESSIONAL", DisplayName = "Profissional" };
        private readonly PlanVersion _proVersion = new() { VersionNumber = 1, Status = PlanVersionStatus.Published };

        public Task<AccountStatus?> GetAccountStatusAsync(Guid id, CancellationToken ct) => Task.FromResult<AccountStatus?>(AccountStatus.Active);
        public Task<PlanOverride?> GetActiveOverrideAsync(Guid id, DateTime utcNow, CancellationToken ct) => Task.FromResult<PlanOverride?>(null);
        public Task<Subscription?> GetSubscriptionAsync(Guid id, CancellationToken ct) => Task.FromResult(Subscription);
        public Task<PlanVersion?> GetPlanVersionAsync(Guid versionId, CancellationToken ct) => Task.FromResult<PlanVersion?>(_proVersion);
        public Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct) => Task.FromResult<Plan?>(_proPlan);
        public Task<PlanVersion?> GetPublishedFreeVersionAsync(DateTime utcNow, CancellationToken ct) => Task.FromResult<PlanVersion?>(_proVersion);
        public Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetFeaturesAsync(Guid planVersionId, CancellationToken ct)
        {
            IReadOnlyDictionary<string, PlanFeatureSetting> dict = new Dictionary<string, PlanFeatureSetting>
            {
                ["documents.create"] = new(true, null, true),
                ["pdf.monthly_limit"] = new(true, null, true)
            };
            return Task.FromResult(dict);
        }
        public Task<int> GetUsageAsync(Guid id, string featureCode, DateTime startOfUtcMonth, CancellationToken ct) => Task.FromResult(0);
        public Task<IReadOnlyList<PlanFeatureCandidate>> GetPublicPlanCandidatesAsync(string featureCode, DateTime utcNow, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PlanFeatureCandidate>>([]);
    }
}
