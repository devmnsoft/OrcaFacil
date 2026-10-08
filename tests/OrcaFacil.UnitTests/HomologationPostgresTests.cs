using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Infrastructure.Pdf;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Repositories;
using OrcaFacil.Persistence.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// Prova de PostgreSQL com duas conexões. Não usa UnitOfWork falso.
/// Só executa quando ORCAFACIL_JOURNEY_CONNECTION aponta para um banco local descartável.
/// </summary>
public sealed class HomologationPostgresTests
{
    [JourneyPostgresFact]
    public async Task Concurrent_apply_links_one_document()
    {
        await using var fx = await Fixture.CreateAsync();
        var reviewId = await fx.SaveReviewAsync();
        var left = fx.ApplyAsync();
        var right = fx.ApplyAsync();
        var results = await Task.WhenAll(Settle(left), Settle(right));
        var winners = results.Where(x => x.Succeeded).Select(x => x.DocumentId).Distinct().ToArray();
        Assert.True(winners.Length == 1, string.Join(" | ", results.Select(x => x.Error ?? "ok")));
        await using var check = fx.Open();
        var card = await check.AiSuggestionCards.AsNoTracking().SingleAsync(x => x.Id == reviewId);
        var documents = await check.Documents.AsNoTracking()
            .Where(x => x.AccountId == fx.AccountId && x.LastAutosaveKey == "ai-apply:" + reviewId.ToString("N") && !x.IsDeleted)
            .ToListAsync();
        Assert.Equal("Applied", card.Status);
        Assert.Equal(winners[0], card.AppliedDocumentId);
        Assert.Single(documents);
        Assert.Equal(winners[0], documents[0].Id);
        var replay = await fx.ApplyAsync();
        Assert.True(replay.Succeeded, replay.Error);
        Assert.Equal(winners[0], replay.DocumentId);
    }

    [JourneyPostgresFact]
    public async Task Apply_and_dismiss_leave_a_single_outcome()
    {
        await using var fx = await Fixture.CreateAsync();
        var reviewId = await fx.SaveReviewAsync();
        var apply = Settle(fx.ApplyAsync());
        var dismiss = SettleDismiss(fx);
        await Task.WhenAll(apply, dismiss);
        await using var check = fx.Open();
        var card = await check.AiSuggestionCards.AsNoTracking().SingleAsync(x => x.Id == reviewId);
        var documents = await check.Documents.CountAsync(x => x.AccountId == fx.AccountId && !x.IsDeleted);
        Assert.Contains(card.Status, new[] { "Applied", "Dismissed" });
        if (card.Status == "Applied")
        {
            Assert.Equal(1, documents);
            Assert.NotNull(card.AppliedDocumentId);
        }
        else
        {
            Assert.Equal(0, documents);
            Assert.Null(card.AppliedDocumentId);
        }
    }

    [JourneyPostgresFact]
    public async Task Two_reservations_at_the_plan_limit_allow_one()
    {
        await using var fx = await Fixture.CreateAsync();
        var first = NamedReserve(fx, "corr-a");
        var second = NamedReserve(fx, "corr-b");
        var results = await Task.WhenAll(first, second);
        Assert.Equal(1, results.Count(x => x.Result.Allowed));
        Assert.Equal(1, results.Count(x => !x.Result.Allowed));
        var winner = results.Single(x => x.Result.Allowed).Key;
        var replay = await fx.ReserveAsync(winner);
        Assert.True(replay.Allowed, replay.Reason);
        await using var check = fx.Open();
        var monthKey = "M:" + DateTime.UtcNow.ToString("yyyyMM");
        var used = await check.Database.SqlQueryRaw<int>(
            "SELECT used AS \"Value\" FROM orcafacil.ai_quota_buckets WHERE account_id = {0} AND period_key = {1}",
            fx.AccountId, monthKey).SingleAsync();
        Assert.Equal(1, used);
        var logsBefore = await check.AiUsageLogs.CountAsync(x => x.AccountId == fx.AccountId);
        await fx.RecordAsync("corr-a");
        await fx.RecordAsync("corr-a");
        await using var after = fx.Open();
        var logs = await after.AiUsageLogs.Where(x => x.AccountId == fx.AccountId).ToListAsync();
        Assert.Equal(logsBefore + 1, logs.Count);
        Assert.All(logs, x => Assert.DoesNotContain("chave", x.SanitizedError ?? "", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(string Key, AiQuotaReservation Result)> NamedReserve(Fixture fx, string key) =>
        (key, await fx.ReserveAsync(key));

    private static async Task<BudgetSuggestionApplyResult> Settle(Task<BudgetSuggestionApplyResult> task)
    {
        try { return await task; }
        catch (Exception ex)
        {
            var state = "";
            var text = ex.GetType().Name;
            for (var current = ex; current is not null; current = current.InnerException)
            {
                var sql = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
                var message = current.GetType().GetProperty("MessageText")?.GetValue(current) as string;
                if (sql is not null) state = sql;
                if (message is not null) text = message;
            }
            return BudgetSuggestionApplyResult.Fail(state + " " + text);
        }
    }

    private static async Task<bool> SettleDismiss(Fixture fx)
    {
        try { return await fx.DismissAsync(); }
        catch (Exception) { return false; }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string connection;
        private readonly Guid userId;
        private readonly Guid serviceId;
        private Guid reviewId;
        public Guid AccountId { get; }

        private Fixture(string connection, Guid accountId, Guid userId, Guid serviceId)
        {
            this.connection = connection;
            AccountId = accountId;
            this.userId = userId;
            this.serviceId = serviceId;
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = Environment.GetEnvironmentVariable("ORCAFACIL_JOURNEY_CONNECTION")
                ?? throw new InvalidOperationException("Conexão ausente.");
            var account = new BusinessAccount { DisplayName = "Conta homologação", Email = $"h-{Guid.NewGuid():N}@exemplo.com", PersonType = PersonType.Individual };
            var user = new UserAccount { Name = "Homologação", Email = $"u-{Guid.NewGuid():N}@exemplo.com", PasswordHash = "hash-de-teste", Role = UserRole.User, Plan = PlanType.Free };
            var service = new ServiceCatalogItem { AccountId = account.Id, Name = "Instalação de teste", UnitCode = "un", StandardPrice = 150m, IsActive = true };
            await using var db = new OrcaFacilDbContext(ContextOptions(connection));
            db.AddRange(account, user, service);
            await db.SaveChangesAsync();
            return new Fixture(connection, account.Id, user.Id, service.Id);
        }

        public async Task<Guid> SaveReviewAsync()
        {
            await using var db = Open();
            reviewId = await new AiSuggestionReviewService(db).SavePendingAsync(AccountId, userId, new BudgetAiSuggestionResult(
                true, "Escopo de teste", "Notas", [new BudgetAiItemSuggestion(serviceId, "Instalação de teste", 2m, 150m, 300m, "un", true)],
                300m, false, "Revisão"), CancellationToken.None);
            return reviewId;
        }

        public async Task<BudgetSuggestionApplyResult> ApplyAsync()
        {
            await using var db = Open();
            return await ApplyOn(db).ApplyAsync(userId, AccountId, reviewId, [new BudgetSuggestionApplyItem(serviceId, 2m)], CancellationToken.None);
        }

        public async Task<bool> DismissAsync()
        {
            await using var db = Open();
            return await new AiSuggestionReviewService(db).MarkAsync(AccountId, reviewId, "Dismissed", CancellationToken.None);
        }

        public async Task<AiQuotaReservation> ReserveAsync(string correlation)
        {
            await using var db = Open();
            return await new AiConsumptionService(db, Microsoft.Extensions.Options.Options.Create(new AiOptions { MonthlyAccountLimit = 200, DailyUserLimit = 5 }), new LimitPlan(1))
                .TryReserveAsync(AccountId, userId, "budget-suggest", correlation, CancellationToken.None);
        }

        public async Task RecordAsync(string correlation)
        {
            await using var db = Open();
            await new AiConsumptionService(db, Microsoft.Extensions.Options.Options.Create(new AiOptions { MonthlyAccountLimit = 200, DailyUserLimit = 5 }), new LimitPlan(1))
                .RecordAsync(new AiUsageEntry(AccountId, userId, "budget-suggest", "none", "ExternalProvider", 10, 0m, 5, "Succeeded", null, correlation), CancellationToken.None);
        }

        public OrcaFacilDbContext Open() => new(ContextOptions(connection));

        public async ValueTask DisposeAsync()
        {
            await using var db = Open();
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.document_items WHERE document_id IN (SELECT id FROM orcafacil.documents WHERE account_id = {0})", AccountId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.documents WHERE account_id = {0}", AccountId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.ai_suggestion_cards WHERE account_id = {0}", AccountId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.ai_usage_logs WHERE account_id = {0}", AccountId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.ai_quota_reservations WHERE account_id = {0}", AccountId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.ai_quota_buckets WHERE account_id = {0}", AccountId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.service_catalog_items WHERE account_id = {0}", AccountId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.users WHERE id = {0}", userId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM orcafacil.business_accounts WHERE id = {0}", AccountId);
        }

        private BudgetSuggestionApplyService ApplyOn(OrcaFacilDbContext db)
        {
            var documents = new EfRepository<Document>(db);
            var unitOfWork = new UnitOfWork(db);
            var wizard = new BudgetWizardService(
                documents,
                new EfRepository<DocumentItem>(db),
                new EfRepository<Client>(db),
                new EfRepository<ServiceCatalogItem>(db),
                new EfRepository<BudgetTemplate>(db),
                new EfRepository<BudgetTemplateItem>(db),
                new EfRepository<AccountSettings>(db),
                unitOfWork,
                new DocumentNumberService(documents, NullLogger<DocumentNumberService>.Instance));
            return new BudgetSuggestionApplyService(new AiSuggestionReviewService(db), wizard, documents, unitOfWork);
        }

        private static DbContextOptions<OrcaFacilDbContext> ContextOptions(string connection) =>
            new DbContextOptionsBuilder<OrcaFacilDbContext>().UseNpgsql(connection).EnableSensitiveDataLogging(false).Options;
    }

    private sealed class LimitPlan(int limit) : IPlanAccessService
    {
        public Task<Subscription?> GetCurrentSubscriptionAsync(Guid accountId, CancellationToken ct = default) => Task.FromResult<Subscription?>(null);
        public Task<Plan?> GetSelectedPlanAsync(Guid accountId, CancellationToken ct = default) => Task.FromResult<Plan?>(null);
        public Task<Plan?> GetEffectivePlanAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<Plan?>(null);
        public Task<PlanVersion?> GetEffectivePlanVersionAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<PlanVersion?>(null);
        public Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetPlanFeaturesAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<string, PlanFeatureSetting>>(new Dictionary<string, PlanFeatureSetting>());
        public Task<PlanAccessDecision> CanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default) =>
            Task.FromResult(new PlanAccessDecision(true, featureCode, "PRO", null, 0, limit, "Limite de teste.", "allowed"));
        public Task<int?> GetLimitAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult<int?>(limit);
        public Task<int> GetUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int?> GetRemainingUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult<int?>(limit);
        public Task EnsureCanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateAccountCacheAsync(Guid accountId, CancellationToken ct = default) => Task.CompletedTask;
    }
}

public sealed class HomologationPdfSamples
{
    [Fact]
    public async Task Writes_budget_and_receipt_samples_when_the_output_directory_is_set()
    {
        var dir = Environment.GetEnvironmentVariable("ORCAFACIL_PDF_SAMPLE_DIR");
        if (string.IsNullOrWhiteSpace(dir)) return;
        Directory.CreateDirectory(dir);
        var service = new QuestPdfDocumentService();
        await Write(service, dir, "orcamento-curto-sem-logo.pdf", Sample(DocumentType.Budget, "ORC-000003", 1, "Instalação elétrica", 180m, 0m, null), PlanType.Professional);
        await Write(service, dir, "orcamento-muitos-itens.pdf", Sample(DocumentType.Budget, "ORC-000048", 48, "Manutenção preventiva com descrição longa o bastante para ocupar mais de uma linha no quadro de itens e forçar a quebra de página.", 1250.5m, 80m, LongTerms()), PlanType.Free);
        await Write(service, dir, "orcamento-valor-alto-acentos.pdf", Sample(DocumentType.Budget, "ORC-000099", 3, "Revisão técnica — ação, ç e ã. O valor é elevado.", 9999999.99m, 1500m, "Condições: pagamento à vista, garantia de 90 dias e escopo sem dados pessoais."), PlanType.Professional);
        await Write(service, dir, "recibo-curto.pdf", Sample(DocumentType.Receipt, "REC-000001", 2, "Recibo de serviço genérico", 640m, 40m, "Recebimento sem identificação de pessoa real."), PlanType.Free);
        foreach (var file in Directory.GetFiles(dir, "*.pdf"))
        {
            var bytes = await File.ReadAllBytesAsync(file);
            Assert.True(bytes.Length > 1000, file);
            Assert.Equal(0x25, bytes[0]);
        }
    }

    private static async Task Write(QuestPdfDocumentService service, string dir, string name, Document document, PlanType plan)
    {
        var pdf = await service.GenerateDocumentPdfAsync(document, new IssuerProfile { BusinessName = "Emitente de exemplo", Email = "contato@exemplo.com" }, plan, CancellationToken.None);
        await File.WriteAllBytesAsync(Path.Combine(dir, name), pdf);
    }

    private static Document Sample(DocumentType type, string number, int items, string description, decimal unitPrice, decimal discount, string? notes)
    {
        var document = new Document
        {
            Type = type,
            Status = "Approved",
            ClientName = "Cliente de exemplo",
            ClientCity = "Recife",
            Notes = notes,
            Discount = discount
        };
        document.IssueNumber(number);
        for (var i = 1; i <= items; i++)
            document.Items.Add(new DocumentItem { Description = $"{i:00}. {description}", Unit = "un", Quantity = i == 1 ? 1.5m : 1m, UnitPrice = unitPrice, Discount = i == 1 ? 10m : 0m });
        document.CalculateTotals();
        return document;
    }

    private static string LongTerms()
    {
        var line = "Condição comercial de exemplo, sem nome, documento ou endereço de pessoa real. ";
        return string.Concat(Enumerable.Repeat(line, 30));
    }
}
