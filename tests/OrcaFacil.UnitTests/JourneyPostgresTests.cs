using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Infrastructure.Pdf;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class JourneyPostgresFactAttribute : FactAttribute
{
    public JourneyPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORCAFACIL_JOURNEY_CONNECTION")))
            Skip = "ORCAFACIL_JOURNEY_CONNECTION ausente. Esta ausência não comprova a jornada no PostgreSQL.";
    }
}

public sealed class JourneyPostgresTests
{
    [JourneyPostgresFact]
    public async Task Partial_payoff_and_concurrent_receipts_stay_within_balance()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var order = await fx.SeedWorkOrderAsync(1000m);
        var first = await fx.Service().RegisterAsync(fx.Payment(order, 300m, "parcial-300"));
        var second = await fx.Service().RegisterAsync(fx.Payment(order, 700m, "quitacao-700"));
        var extra = await fx.Service().RegisterAsync(fx.Payment(order, 1m, "excedente"));
        Assert.True(first.Succeeded, first.Message);
        Assert.Equal("PartiallyPaid", first.Code);
        Assert.True(second.Succeeded, second.Message);
        Assert.Equal("Registered", second.Code);
        Assert.False(extra.Succeeded);
        Assert.Equal("AlreadyPaid", extra.Code);
        Assert.Equal(1000m, await fx.ActivePaidAsync(order));

        var raced = await fx.SeedWorkOrderAsync(1000m);
        var left = fx.Service().RegisterAsync(fx.Payment(raced, 600m, "corrida-a"));
        var right = fx.Service().RegisterAsync(fx.Payment(raced, 600m, "corrida-b"));
        var results = await Task.WhenAll(left, right);
        Assert.Equal(1, results.Count(x => x.Succeeded));
        Assert.Contains(results, x => !x.Succeeded);
        var paid = await fx.ActivePaidAsync(raced);
        Assert.True(paid <= 1000m);
        Assert.Equal(600m, paid);
    }

    [JourneyPostgresFact]
    public async Task Idempotency_replays_same_command_and_conflicts_on_different_payload()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var order = await fx.SeedWorkOrderAsync(1000m);
        var paidAt = DateTime.UtcNow.AddMinutes(-2);
        var original = await fx.Service().RegisterAsync(new ManualPaymentRequest(order, 300m, "pix", paidAt, "manual", "chave-1"));
        var replay = await fx.Service().RegisterAsync(new ManualPaymentRequest(order, 300m, "pix", paidAt, "outra nota", "chave-1"));
        var conflict = await fx.Service().RegisterAsync(new ManualPaymentRequest(order, 400m, "pix", paidAt, "manual", "chave-1"));
        Assert.True(original.Succeeded, original.Message);
        Assert.True(replay.Succeeded, replay.Message);
        Assert.Equal("IdempotentReplay", replay.Code);
        Assert.Equal(original.EntityId, replay.EntityId);
        Assert.False(conflict.Succeeded);
        Assert.Equal("IdempotencyConflict", conflict.Code);
        Assert.Equal(300m, await fx.ActivePaidAsync(order));

        var rejected = await fx.Service().RegisterAsync(new ManualPaymentRequest(order, 0m, "pix", paidAt, null, "chave-invalida"));
        var later = await fx.Service().RegisterAsync(new ManualPaymentRequest(order, 100m, "pix", paidAt, null, "chave-invalida"));
        Assert.False(rejected.Succeeded);
        Assert.True(later.Succeeded, later.Message);
        Assert.Equal(400m, await fx.ActivePaidAsync(order));
    }

    [JourneyPostgresFact]
    public async Task Repeated_and_concurrent_conversion_creates_one_work_order()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var document = await fx.SeedApprovedQuoteAsync();
        var first = fx.Service().ConvertToWorkOrderAsync(document);
        var second = fx.Service().ConvertToWorkOrderAsync(document);
        var raced = await Task.WhenAll(first, second);
        Assert.All(raced, x => Assert.True(x.Succeeded, x.Message));
        Assert.Equal(raced[0].EntityId, raced[1].EntityId);
        var again = await fx.Service().ConvertToWorkOrderAsync(document);
        Assert.Equal("IdempotentReplay", again.Code);
        Assert.Equal(raced[0].EntityId, again.EntityId);
        await using var check = fx.Context();
        Assert.Equal(1, await check.WorkOrders.CountAsync(x => x.AccountId == fx.AccountId && x.SourceDocumentId == document));
    }

    [JourneyPostgresFact]
    public async Task Conflicting_decisions_keep_the_first_and_old_revision_cannot_decide_the_current_one()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var (token, currentRevision) = await fx.SeedPublicQuoteAsync();
        var approve = fx.Service().DecideAsync(token, PublicDocumentDecisionType.Approved, "Cliente A", "cliente@exemplo.com", null, null, null, true, "decisao-a", "203.0.113.10", "teste");
        var reject = fx.Service().DecideAsync(token, PublicDocumentDecisionType.Rejected, "Cliente A", "cliente@exemplo.com", "preco", null, null, false, "decisao-b", "203.0.113.11", "teste");
        var results = await Task.WhenAll(approve, reject);
        Assert.Equal(1, results.Count(x => x.Succeeded));
        var failed = results.Single(x => !x.Succeeded);
        Assert.True(failed.Code is QuoteLifecycleCode.DecisionAlreadyRegistered or QuoteLifecycleCode.IdempotencyConflict);
        await using var check = fx.Context();
        Assert.Equal(1, await check.PublicDocumentDecisions.CountAsync(x => x.DocumentRevisionId == currentRevision));

        var oldToken = await fx.SeedOutdatedLinkAsync();
        var outdated = await fx.Service().DecideAsync(oldToken, PublicDocumentDecisionType.Approved, "Cliente A", "cliente@exemplo.com", null, null, null, true, "decisao-antiga", "203.0.113.12", "teste");
        Assert.False(outdated.Succeeded);
        Assert.Equal(QuoteLifecycleCode.VersionOutdated, outdated.Code);
    }

    [JourneyPostgresFact]
    public async Task Repeated_and_concurrent_receipts_keep_one_receipt_for_the_amount_received()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var order = await fx.SeedWorkOrderAsync(1000m);
        var payment = await fx.Service().RegisterAsync(fx.Payment(order, 300m, "recibo-300"));
        Assert.True(payment.Succeeded, payment.Message);
        var left = fx.Service().GenerateReceiptAsync(payment.EntityId!.Value);
        var right = fx.Service().GenerateReceiptAsync(payment.EntityId.Value);
        var raced = await Task.WhenAll(left, right);
        Assert.All(raced, x => Assert.True(x.Succeeded, x.Message));
        Assert.Equal(raced[0].EntityId, raced[1].EntityId);
        await using var check = fx.Context();
        var receipt = await check.Receipts.SingleAsync(x => x.PaymentId == payment.EntityId);
        Assert.Equal(300m, receipt.Amount);
        Assert.Contains("não substitui nota fiscal", receipt.FiscalNotice, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await check.Receipts.CountAsync(x => x.AccountId == fx.AccountId && x.PaymentId == payment.EntityId));
    }

    [JourneyPostgresFact]
    public async Task Another_account_cannot_register_a_payment_on_this_work_order()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var order = await fx.SeedWorkOrderAsync(1000m);
        var foreign = await fx.ServiceForOtherAccount().RegisterAsync(fx.Payment(order, 100m, "conta-b"));
        Assert.False(foreign.Succeeded);
        Assert.Equal("NotFound", foreign.Code);
        Assert.Equal(0m, await fx.ActivePaidAsync(order));
    }

    [JourneyPostgresFact]
    public async Task Document_balance_selects_current_revision_order_and_does_not_duplicate_direct_payments()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var seeded = await fx.SeedQuoteWithTwoOrdersAndPaymentsAsync();
        await using var db = fx.Context();
        var balances = new CommercialBalanceService(db);

        var balance = await balances.GetForDocumentAsync(fx.AccountId, seeded.DocumentId);

        Assert.NotNull(balance);
        Assert.Equal(seeded.CurrentOrderId, balance.WorkOrderId);
        Assert.Equal("WorkOrder", balance.ContractSource);
        Assert.Equal(1200m, balance.ContractedAmount);
        Assert.Equal(600m, balance.ReceivedAmount);
        Assert.Equal(50m, balance.ReversedAmount);
        Assert.Equal(600m, balance.BalanceAmount);
        Assert.Contains(balance.Warnings, x => x.Contains("mais de uma ordem", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(balance.Warnings, x => x.Contains("Recebimentos diretos", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(balance.Warnings, x => x.Contains("outra ordem", StringComparison.OrdinalIgnoreCase));
    }

    [JourneyPostgresFact]
    public async Task Work_order_balance_excludes_direct_document_payments_when_order_is_not_contract_origin()
    {
        await using var fx = await JourneyFixture.CreateAsync();
        var seeded = await fx.SeedQuoteWithTwoOrdersAndPaymentsAsync();
        await using var db = fx.Context();
        var balances = new CommercialBalanceService(db);

        var balance = await balances.GetForWorkOrderAsync(fx.AccountId, seeded.OldOrderId);

        Assert.NotNull(balance);
        Assert.Equal(seeded.OldOrderId, balance.WorkOrderId);
        Assert.Equal(1000m, balance.ContractedAmount);
        Assert.Equal(300m, balance.ReceivedAmount);
        Assert.Equal(700m, balance.BalanceAmount);
        Assert.Contains(balance.Warnings, x => x.Contains("não é a origem contratual", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class JourneyFixture : IAsyncDisposable
    {
        private readonly string connection;
        public Guid AccountId { get; }
        public Guid OtherAccountId { get; }
        public Guid UserId { get; }
        private readonly Guid clientId;

        private JourneyFixture(string connection, Guid accountId, Guid otherAccountId, Guid userId, Guid clientId)
        {
            this.connection = connection;
            AccountId = accountId;
            OtherAccountId = otherAccountId;
            UserId = userId;
            this.clientId = clientId;
        }

        public static async Task<JourneyFixture> CreateAsync()
        {
            var connection = Environment.GetEnvironmentVariable("ORCAFACIL_JOURNEY_CONNECTION")
                ?? throw new InvalidOperationException("Conexão ausente.");
            var account = new BusinessAccount { DisplayName = "Conta A", Email = $"a-{Guid.NewGuid():N}@exemplo.com", PersonType = PersonType.Individual };
            var other = new BusinessAccount { DisplayName = "Conta B", Email = $"b-{Guid.NewGuid():N}@exemplo.com", PersonType = PersonType.Individual };
            var user = new UserAccount { Name = "Ana", Email = $"ana-{Guid.NewGuid():N}@exemplo.com", PasswordHash = "hash-de-teste", Role = UserRole.User, Plan = PlanType.Free };
            var client = new Client { AccountId = account.Id, UserId = user.Id, Name = "Cliente da conta A" };
            var member = new AccountMember { AccountId = account.Id, UserId = user.Id, RoleCode = "Owner" };
            member.Join();
            await using var db = new OrcaFacilDbContext(Options(connection));
            db.AddRange(account, other, user);
            await db.SaveChangesAsync();
            db.AddRange(client, member);
            await db.SaveChangesAsync();
            return new JourneyFixture(connection, account.Id, other.Id, user.Id, client.Id);
        }

        public CommercialJourneyService Service() => Build(AccountId);
        public CommercialJourneyService ServiceForOtherAccount() => Build(OtherAccountId);
        public OrcaFacilDbContext Context() => new(Options(connection));

        public ManualPaymentRequest Payment(Guid orderId, decimal amount, string key) =>
            new(orderId, amount, "pix", DateTime.UtcNow.AddMinutes(-1), "Recebimento manual de teste", key + "-" + orderId.ToString("N"));

        public async Task<Guid> SeedWorkOrderAsync(decimal total)
        {
            var order = new WorkOrder
            {
                AccountId = AccountId, ClientId = clientId, Number = "OS-" + Guid.NewGuid().ToString("N")[..12],
                Title = "Serviço de instalação", ClientSnapshot = """{"name":"Cliente da conta A"}""", ItemsSnapshot = "[]",
                TotalSnapshot = total, CreatedByUserId = UserId
            };
            await using var db = Context();
            db.WorkOrders.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        }

        public async Task<Guid> SeedApprovedQuoteAsync()
        {
            var document = NewDocument("Approved");
            var revision = Revision(document, 1, true, 1000m);
            await using var db = Context();
            db.AddRange(document, revision);
            await db.SaveChangesAsync();
            return document.Id;
        }

        public async Task<QuoteWithOrders> SeedQuoteWithTwoOrdersAndPaymentsAsync()
        {
            var document = NewDocument("Approved");
            var oldRevision = Revision(document, 1, false, 1000m);
            var currentRevision = Revision(document, 2, true, 1200m);
            var oldOrder = OrderFrom(document, oldRevision, 1000m, "OS-ANT");
            var currentOrder = OrderFrom(document, currentRevision, 1200m, "OS-ATU");
            var directPayment = Payment(document.Id, null, 200m, "direto");
            var oldOrderPayment = Payment(document.Id, oldOrder.Id, 300m, "ordem-antiga");
            var currentOrderPayment = Payment(document.Id, currentOrder.Id, 400m, "ordem-atual");
            var reversed = Payment(document.Id, currentOrder.Id, 50m, "estornado");
            reversed.Status = FinancialRecordStatus.Reversed;
            reversed.ReversedAt = DateTime.UtcNow;
            reversed.ReversedByUserId = UserId;
            reversed.ReversalReason = "Teste de estorno";

            await using var db = Context();
            db.AddRange(document, oldRevision, currentRevision, oldOrder, currentOrder,
                directPayment, oldOrderPayment, currentOrderPayment, reversed);
            await db.SaveChangesAsync();
            return new(document.Id, oldOrder.Id, currentOrder.Id);
        }

        public async Task<(string Token, Guid RevisionId)> SeedPublicQuoteAsync()
        {
            var document = NewDocument("Sent");
            var revision = Revision(document, 1, true, 1000m);
            var tokens = new PublicDocumentTokenService();
            var created = tokens.Create();
            var access = new PublicDocumentAccess
            {
                AccountId = AccountId, DocumentId = document.Id, DocumentRevisionId = revision.Id,
                TokenHash = tokens.Hash(created.Token), ExpiresAt = DateTime.UtcNow.AddDays(2), CreatedByUserId = UserId
            };
            await using var db = Context();
            db.AddRange(document, revision, access);
            await db.SaveChangesAsync();
            return (created.Token, revision.Id);
        }

        public async Task<string> SeedOutdatedLinkAsync()
        {
            var document = NewDocument("Sent");
            var oldRevision = Revision(document, 1, false, 800m);
            var current = Revision(document, 2, true, 1000m);
            var tokens = new PublicDocumentTokenService();
            var created = tokens.Create();
            var access = new PublicDocumentAccess
            {
                AccountId = AccountId, DocumentId = document.Id, DocumentRevisionId = oldRevision.Id,
                TokenHash = tokens.Hash(created.Token), ExpiresAt = DateTime.UtcNow.AddDays(2), CreatedByUserId = UserId
            };
            await using var db = Context();
            db.AddRange(document, oldRevision, current, access);
            await db.SaveChangesAsync();
            return created.Token;
        }

        public async Task<decimal> ActivePaidAsync(Guid orderId)
        {
            await using var db = Context();
            return await db.ManualPayments.Where(x => x.AccountId == AccountId && x.WorkOrderId == orderId && !x.IsDeleted && x.Status == FinancialRecordStatus.Active)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = Context();
            var payments = await db.ManualPayments.Where(x => x.AccountId == AccountId || x.AccountId == OtherAccountId).ToListAsync();
            var receipts = await db.Receipts.Where(x => x.AccountId == AccountId || x.AccountId == OtherAccountId).ToListAsync();
            db.RemoveRange(receipts);
            await db.SaveChangesAsync();
            db.RemoveRange(payments);
            await db.SaveChangesAsync();
        }

        private CommercialJourneyService Build(Guid accountId)
        {
            var context = Context();
            return new(
                context, new FixedAccount(UserId, accountId), new FixedUser(UserId), new AllowAllPlans(),
                new DocumentSnapshotSerializer(), new PublicDocumentTokenService(), new DocumentStatusTransitionService(),
                new WorkOrderStatusTransitionService(), new NumberToWordsPtBrService(),
                new TechnicalFingerprintService("orcafacil-jornada-teste-pepper-32"),
                new CommercialBalanceService(context));
        }

        private Document NewDocument(string status)
        {
            var document = new Document
            {
                AccountId = AccountId, ClientId = clientId, UserId = UserId, Type = DocumentType.Budget,
                Status = status, ClientName = "Cliente da conta A", IssueDate = DateTime.UtcNow
            };
            document.IssueNumber("ORC-" + Guid.NewGuid().ToString("N")[..10]);
            document.CalculateTotals();
            return document;
        }

        private DocumentRevision Revision(Document document, int version, bool current, decimal total)
        {
            var serialized = new DocumentSnapshotSerializer().Serialize(new DocumentSnapshot(
                new IssuerSnapshot("Emitente", null, null, null, null, "Recife", "PE", null, null, null),
                new CustomerSnapshot("Cliente da conta A", null, null, null, null, null, "Recife", "PE"),
                new QuoteSnapshot(document.Number, document.IssueDate, null, null, "Pix", "À vista", null, "essential", null, null, false, total, 0m, total),
                [new QuoteItemSnapshot("Instalação", "un", 1m, total, 0m, total, total)]));
            return new DocumentRevision
            {
                AccountId = AccountId, DocumentId = document.Id, VersionNumber = version, Status = DocumentRevisionStatus.Approved,
                CreatedByUserId = UserId, SnapshotHash = serialized.Hash, ProtectedSnapshot = serialized.Json,
                Total = total, IsCurrent = current
            };
        }

        private WorkOrder OrderFrom(Document document, DocumentRevision revision, decimal total, string prefix) =>
            new()
            {
                AccountId = AccountId,
                ClientId = clientId,
                SourceDocumentId = document.Id,
                SourceRevisionId = revision.Id,
                Number = prefix + "-" + Guid.NewGuid().ToString("N")[..8],
                Title = "Serviço vinculado à proposta",
                ClientSnapshot = """{"name":"Cliente da conta A"}""",
                ItemsSnapshot = "[]",
                TotalSnapshot = total,
                CreatedByUserId = UserId
            };

        private ManualPayment Payment(Guid documentId, Guid? orderId, decimal amount, string suffix) =>
            new()
            {
                AccountId = AccountId,
                DocumentId = documentId,
                WorkOrderId = orderId,
                ClientId = clientId,
                Amount = amount,
                PaymentMethod = "pix",
                PaidAt = DateTime.UtcNow.AddMinutes(-1),
                RegisteredByUserId = UserId,
                IdempotencyKey = suffix + "-" + Guid.NewGuid().ToString("N")
            };

        private static DbContextOptions<OrcaFacilDbContext> Options(string connection) =>
            new DbContextOptionsBuilder<OrcaFacilDbContext>().UseNpgsql(connection).EnableSensitiveDataLogging(false).Options;

        public sealed record QuoteWithOrders(Guid DocumentId, Guid OldOrderId, Guid CurrentOrderId);
    }

    private sealed class FixedAccount(Guid userId, Guid accountId) : ICurrentAccountService
    {
        public Guid UserId => userId;
        public Guid? AccountId => accountId;
        public Guid? AccountMemberId => null;
        public string? AccountRoleCode => "Owner";
        public AccountStatus? AccountStatus => Domain.Enums.AccountStatus.Active;
        public bool IsPlatformUser => false;
        public bool HasAccount => true;
        public Task<bool> HasPermissionAsync(string permissionCode, CancellationToken ct = default) => Task.FromResult(true);
        public Task EnsureAccountAccessAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FixedUser(Guid userId) : ICurrentUserService
    {
        public Guid UserId => userId;
        public Guid? TryGetUserId() => userId;
        public string? Email => "ana@exemplo.com";
        public string? Name => "Ana";
        public string? Role => "User";
        public string? Plan => "Free";
        public bool IsAuthenticated => true;
        public bool IsSuperAdmin => false;
    }

    private sealed class AllowAllPlans : IPlanAccessService
    {
        private static PlanAccessDecision Allow(string feature) => new(true, feature, "FREE", null, 0, null, "Permitido no teste.", "allowed");
        public Task<Subscription?> GetCurrentSubscriptionAsync(Guid accountId, CancellationToken ct = default) => Task.FromResult<Subscription?>(null);
        public Task<Plan?> GetSelectedPlanAsync(Guid accountId, CancellationToken ct = default) => Task.FromResult<Plan?>(null);
        public Task<Plan?> GetEffectivePlanAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<Plan?>(null);
        public Task<PlanVersion?> GetEffectivePlanVersionAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<PlanVersion?>(null);
        public Task<IReadOnlyDictionary<string, PlanFeatureSetting>> GetPlanFeaturesAsync(Guid accountId, DateTime utcNow, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<string, PlanFeatureSetting>>(new Dictionary<string, PlanFeatureSetting>());
        public Task<PlanAccessDecision> CanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult(Allow(featureCode));
        public Task<int?> GetLimitAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult<int?>(null);
        public Task<int> GetUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int?> GetRemainingUsageAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.FromResult<int?>(null);
        public Task EnsureCanUseAsync(Guid accountId, string featureCode, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateAccountCacheAsync(Guid accountId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
