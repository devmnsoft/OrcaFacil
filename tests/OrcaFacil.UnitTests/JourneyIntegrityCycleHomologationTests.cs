using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.Plans;
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
    public async Task TrialPro_BeforeAndAfterWorker_YieldsIdenticalExpiredDecision()
    {
        var accountId = Guid.NewGuid();

        // 1. ANTES do worker rodar: subscription em Trial com data expirada
        var beforeWorkerDataSource = new MockPlanAccessDataSource(accountId)
        {
            Subscription = new Subscription
            {
                AccountId = accountId,
                Status = SubscriptionStatus.Trial,
                TrialStatus = TrialStatus.Active,
                TrialStartedAt = DateTime.UtcNow.AddDays(-16),
                TrialEndsAt = DateTime.UtcNow.AddDays(-1),
                ManualReleaseUntil = null
            }
        };
        var serviceBefore = new PlanAccessService(beforeWorkerDataSource);
        var decisionBefore = await serviceBefore.CanUseAsync(accountId, "documents.create");

        // 2. DEPOIS do worker rodar: subscription em Expired
        var afterWorkerDataSource = new MockPlanAccessDataSource(accountId)
        {
            Subscription = new Subscription
            {
                AccountId = accountId,
                Status = SubscriptionStatus.Expired,
                TrialStatus = TrialStatus.Expired,
                TrialStartedAt = DateTime.UtcNow.AddDays(-16),
                TrialEndsAt = DateTime.UtcNow.AddDays(-1),
                ManualReleaseUntil = null
            }
        };
        var serviceAfter = new PlanAccessService(afterWorkerDataSource);
        var decisionAfter = await serviceAfter.CanUseAsync(accountId, "documents.create");

        Assert.False(decisionBefore.IsAllowed);
        Assert.False(decisionAfter.IsAllowed);
        Assert.Equal(decisionBefore.InternalReason, decisionAfter.InternalReason);
        Assert.Equal("TrialExpired", decisionAfter.InternalReason);
        Assert.Equal(decisionBefore.UserMessage, decisionAfter.UserMessage);

        // Garante que o plano efetivo não volta a ser Free com permissões de criação
        var versionAfter = await serviceAfter.GetEffectivePlanVersionAsync(accountId, DateTime.UtcNow);
        Assert.Null(versionAfter);
    }

    [Fact]
    public async Task TrialPro_PaidSubscriptionApprovedDuringTrial_KeepsPaidAccess()
    {
        var accountId = Guid.NewGuid();
        var dataSource = new MockPlanAccessDataSource(accountId)
        {
            Subscription = new Subscription
            {
                AccountId = accountId,
                Status = SubscriptionStatus.Active, // Pagamento aprovado durante trial
                Plan = PlanType.Professional,
                TrialStatus = TrialStatus.Active,
                TrialStartedAt = DateTime.UtcNow.AddDays(-5),
                TrialEndsAt = DateTime.UtcNow.AddDays(10),
                PaidThroughAt = DateTime.UtcNow.AddMonths(1)
            }
        };

        var service = new PlanAccessService(dataSource);
        var isExpired = PlanAccessService.IsTrialExpired(dataSource.Subscription, DateTime.UtcNow);
        Assert.False(isExpired);

        var decision = await service.CanUseAsync(accountId, "documents.create");
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task TrialPro_PaidSubscriptionWithExpiredTrialHistory_KeepsPaidAccess()
    {
        var accountId = Guid.NewGuid();
        var dataSource = new MockPlanAccessDataSource(accountId)
        {
            Subscription = new Subscription
            {
                AccountId = accountId,
                Status = SubscriptionStatus.Active,
                Plan = PlanType.Professional,
                TrialStatus = TrialStatus.Expired,
                TrialStartedAt = DateTime.UtcNow.AddDays(-30),
                TrialEndsAt = DateTime.UtcNow.AddDays(-15),
                PaidThroughAt = DateTime.UtcNow.AddMonths(1)
            }
        };

        var service = new PlanAccessService(dataSource);

        Assert.False(PlanAccessService.IsTrialExpired(dataSource.Subscription, DateTime.UtcNow));
        var decision = await service.CanUseAsync(accountId, "documents.create");

        Assert.True(decision.IsAllowed);
        Assert.Equal("Allowed", decision.InternalReason);
    }

    [Fact]
    public async Task TrialPro_Expired_AllowsHistoricalQueriesButBlocksCommercialCreation()
    {
        var accountId = Guid.NewGuid();
        var dataSource = new MockPlanAccessDataSource(accountId)
        {
            Subscription = new Subscription
            {
                AccountId = accountId,
                Status = SubscriptionStatus.Expired,
                TrialStatus = TrialStatus.Expired,
                TrialStartedAt = DateTime.UtcNow.AddDays(-30),
                TrialEndsAt = DateTime.UtcNow.AddDays(-15)
            }
        };

        var service = new PlanAccessService(dataSource);

        var history = await service.CanUseAsync(accountId, PlanFeatureCodes.HistoryDaysVisible);
        var creation = await service.CanUseAsync(accountId, "documents.create");

        Assert.True(history.IsAllowed);
        Assert.Equal("HistoricalAccessAllowed", history.InternalReason);
        Assert.False(creation.IsAllowed);
        Assert.Equal("TrialExpired", creation.InternalReason);
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
    public async Task QuestPdfGenerator_CorruptedOrTraversalLogo_KeepsPdfFunctional()
    {
        var generator = new QuestPdfDocumentService();
        var accountId = Guid.NewGuid();
        var doc = new Document
        {
            AccountId = accountId,
            Type = DocumentType.Budget,
            ClientName = "Cliente Teste",
            IssueDate = DateTime.UtcNow
        };
        doc.IssueNumber("ORC-2026-0002");
        doc.Items.Add(new DocumentItem
        {
            Description = "Item Seguro",
            Quantity = 1,
            UnitPrice = 250m
        });
        doc.CalculateTotals();

        // Tentativa de path traversal
        var issuerTraversal = new IssuerProfile
        {
            BusinessName = "Empresa Segura",
            LogoPath = "../../../../Windows/System32/calc.exe"
        };
        var bytesTraversal = await generator.GenerateDocumentPdfAsync(doc, issuerTraversal, PlanType.Professional);
        Assert.NotNull(bytesTraversal);
        Assert.True(bytesTraversal.Length > 100);

        // Tentativa de acessar branding de outra conta comercial
        var foreignAccount = Guid.NewGuid();
        var issuerForeign = new IssuerProfile
        {
            BusinessName = "Empresa Segura",
            LogoPath = $"/uploads/branding/{foreignAccount:N}/logo.png"
        };
        var bytesForeign = await generator.GenerateDocumentPdfAsync(doc, issuerForeign, PlanType.Professional);
        Assert.NotNull(bytesForeign);
        Assert.True(bytesForeign.Length > 100);
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

    [Fact]
    public void NumberToWordsService_OutputsAccurateCurrencyText()
    {
        var service = new NumberToWordsPtBrService();
        Assert.Equal("um real", service.ToCurrencyWords(1m));
        Assert.Equal("dois reais", service.ToCurrencyWords(2m));
        Assert.Equal("um mil e quinhentos reais", service.ToCurrencyWords(1500m).ToLowerInvariant());
        Assert.Equal("dois reais e cinquenta centavos", service.ToCurrencyWords(2.50m).ToLowerInvariant());
    }

    [Fact]
    public void ManualPaymentIdempotency_MatchesFullPayloadAndDetectsMismatches()
    {
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();
        var docA = Guid.NewGuid();
        var docB = Guid.NewGuid();
        var workOrderA = Guid.NewGuid();
        var paidAt = DateTime.UtcNow;

        // Mesmos parâmetros exatos
        Assert.True(ManualPaymentIdempotency.Matches(
            clientA, docA, workOrderA, 500m, "pix", paidAt,
            clientA, docA, workOrderA, 500m, "pix", paidAt));

        // Cliente divergente
        Assert.False(ManualPaymentIdempotency.Matches(
            clientA, docA, workOrderA, 500m, "pix", paidAt,
            clientB, docA, workOrderA, 500m, "pix", paidAt));

        // Documento divergente
        Assert.False(ManualPaymentIdempotency.Matches(
            clientA, docA, workOrderA, 500m, "pix", paidAt,
            clientA, docB, workOrderA, 500m, "pix", paidAt));

        // Valor divergente
        Assert.False(ManualPaymentIdempotency.Matches(
            clientA, docA, workOrderA, 500m, "pix", paidAt,
            clientA, docA, workOrderA, 600m, "pix", paidAt));

        // Método de pagamento divergente
        Assert.False(ManualPaymentIdempotency.Matches(
            clientA, docA, workOrderA, 500m, "pix", paidAt,
            clientA, docA, workOrderA, 500m, "cash", paidAt));
    }

    [Fact]
    public void CommercialRevisionResolver_UsesSnapshotAsImmutableSource()
    {
        var accountId = Guid.NewGuid();
        var serializer = new DocumentSnapshotSerializer();
        var snapshot = new DocumentSnapshot(
            new IssuerSnapshot("Emitente congelado", "18160057000113", "old@example.com", "11999999999", "Rua A", "Belém", "PA", "/uploads/branding/logo-v1.png", "pix-old", null),
            new CustomerSnapshot("Cliente aprovado", "Company", "12345678000190", "11988887777", "client@example.com", "Rua B", "Recife", null),
            new QuoteSnapshot("ORC-REV-001", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(15), "10 dias", "Pix", "À vista com garantia aprovada", "Observação aprovada", "premium", "#111111", "Garantia de 90 dias", false, 900m, 0m, 900m, "en-US", "BRL"),
            [new QuoteItemSnapshot("Serviço aprovado", "h", 2m, 500m, 100m, 1000m, 900m)]);
        var serialized = serializer.Serialize(snapshot);
        var mutableDocument = new Document
        {
            AccountId = accountId,
            Type = DocumentType.Budget,
            ClientName = "Cliente alterado",
            ConditionsText = "Condição mutável",
            TemplateCode = "essential"
        };
        mutableDocument.IssueNumber("ORC-MUTABLE");
        mutableDocument.Items.Add(new DocumentItem { Description = "Item mutável", Quantity = 1, UnitPrice = 1m });
        mutableDocument.CalculateTotals();
        var revision = new DocumentRevision
        {
            AccountId = accountId,
            DocumentId = mutableDocument.Id,
            VersionNumber = 3,
            IsCurrent = true,
            ProtectedSnapshot = serialized.Json,
            SnapshotHash = serialized.Hash,
            Total = 900m
        };

        var result = new CommercialRevisionResolver().Resolve(
            mutableDocument,
            revision,
            new IssuerProfile { BusinessName = "Emitente atual" },
            PlanType.Free);

        Assert.True(result.Succeeded, $"{result.Code}: {result.Message}");
        Assert.True(result.Value!.IsSnapshotSource);
        Assert.Equal("ORC-REV-001", result.Value.Document.Number);
        Assert.Equal("Cliente aprovado", result.Value.Document.ClientName);
        Assert.Equal("À vista com garantia aprovada", result.Value.Document.ConditionsText);
        var item = Assert.Single(result.Value.Document.Items);
        Assert.Equal("Serviço aprovado", item.Description);
        Assert.Equal(900m, result.Value.Document.Subtotal);
        Assert.Equal(100m, item.Discount);
        Assert.Equal(0m, result.Value.Document.Discount);
        Assert.Equal(900m, result.Value.Document.Total);
        Assert.Equal("Emitente congelado", result.Value.Issuer.BusinessName);
        Assert.Equal("en-US", result.Value.LanguageCode);
        Assert.Equal("BRL", result.Value.CurrencyCode);
        Assert.Equal(PlanType.Professional, result.Value.EffectivePlan);
    }

    [Fact]
    public void CommercialRevisionResolver_RejectsInconsistentSnapshotTotals()
    {
        var accountId = Guid.NewGuid();
        var serializer = new DocumentSnapshotSerializer();
        var snapshot = new DocumentSnapshot(
            new IssuerSnapshot("Emitente", null, null, null, null, null, null, null, null, null),
            new CustomerSnapshot("Cliente", null, null, null, null, null, null, null),
            new QuoteSnapshot("ORC-BAD", DateTime.UtcNow.Date, null, null, null, null, null, "essential", null, null, false, 1000m, 100m, 900m, "pt-BR", "BRL"),
            [new QuoteItemSnapshot("Serviço", "un", 1m, 1000m, 100m, 1000m, 900m)]);
        var serialized = serializer.Serialize(snapshot);

        var result = new CommercialRevisionResolver().Resolve(
            new Document { AccountId = accountId, Type = DocumentType.Budget },
            new DocumentRevision { AccountId = accountId, VersionNumber = 1, ProtectedSnapshot = serialized.Json, SnapshotHash = serialized.Hash, Total = 900m },
            new IssuerProfile { BusinessName = "Emitente atual" },
            PlanType.Professional);

        Assert.False(result.Succeeded);
        Assert.Equal("InvalidSnapshot", result.Code);
    }

    [Fact]
    public void CommercialRevisionResolver_RejectsInvalidProtectedSnapshot()
    {
        var accountId = Guid.NewGuid();
        var result = new CommercialRevisionResolver().Resolve(
            new Document { AccountId = accountId, Type = DocumentType.Budget },
            new DocumentRevision { AccountId = accountId, VersionNumber = 1, ProtectedSnapshot = "{invalid-json" },
            new IssuerProfile { BusinessName = "Emitente atual" },
            PlanType.Professional);

        Assert.False(result.Succeeded);
        Assert.Equal("InvalidSnapshot", result.Code);
    }

    private sealed class MockPlanAccessDataSource(Guid accountId) : IPlanAccessDataSource
    {
        public Subscription? Subscription { get; set; }
        private readonly Guid _accountId = accountId;
        private readonly Plan _proPlan = new() { Code = "PROFESSIONAL", DisplayName = "Profissional" };
        private readonly PlanVersion _proVersion = new() { VersionNumber = 1, Status = PlanVersionStatus.Published };

        public Task<AccountStatus?> GetAccountStatusAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<AccountStatus?>(id == _accountId ? AccountStatus.Active : null);
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
                ["pdf.monthly_limit"] = new(true, null, true),
                [PlanFeatureCodes.HistoryDaysVisible] = new(true, 365, false),
                [PlanFeatureCodes.BasicReportsEnabled] = new(true, null, true)
            };
            return Task.FromResult(dict);
        }
        public Task<int> GetUsageAsync(Guid id, string featureCode, DateTime startOfUtcMonth, CancellationToken ct) => Task.FromResult(0);
        public Task<IReadOnlyList<PlanFeatureCandidate>> GetPublicPlanCandidatesAsync(string featureCode, DateTime utcNow, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PlanFeatureCandidate>>([]);
    }
}
