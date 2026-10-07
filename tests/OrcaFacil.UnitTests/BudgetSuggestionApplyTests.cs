using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class BudgetSuggestionApplyTests
{
    [Fact]
    public async Task Apply_creates_draft_preserving_quantities_and_links_the_review()
    {
        var (service, apply, documents, items, reviews, accountId, userId) = await Scenario(itemQuantity: 3m);

        var result = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id,
            [new(service.Id, null)], CancellationToken.None);

        Assert.True(result.Succeeded);
        var document = Assert.Single(documents.Items);
        Assert.Equal(result.DocumentId, document.Id);
        var item = Assert.Single(items.Items);
        Assert.Equal(document.Id, item.DocumentId);
        Assert.Equal(service.Id, item.ServiceCatalogItemId);
        Assert.Equal(3m, item.Quantity);
        Assert.Equal("Applied", reviews.Review!.Status);
        Assert.Equal(document.Id, reviews.Review.AppliedDocumentId);
        Assert.False(string.IsNullOrWhiteSpace(reviews.Review.ApplyFingerprint));
    }

    [Fact]
    public async Task Repeated_apply_with_same_selection_returns_the_same_document()
    {
        var (service, apply, documents, reviews, accountId, userId) = await Scenario();

        var first = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);
        var second = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.DocumentId, second.DocumentId);
        Assert.Single(documents.Items);
        Assert.Equal(1, reviews.SuccessfulClaims);
    }

    [Fact]
    public async Task Repeated_apply_with_different_selection_conflicts()
    {
        var (service, apply, documents, reviews, accountId, userId) = await Scenario();

        var first = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);
        var second = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, 5m)], CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.True(second.Conflict);
        Assert.Single(documents.Items);
    }

    [Fact]
    public async Task Catalog_price_change_after_suggestion_blocks_apply_without_silent_swap()
    {
        var (service, apply, documents, reviews, accountId, userId) = await Scenario();
        service.StandardPrice = 999m;

        var result = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.Conflict);
        Assert.Contains("mudou", result.Error);
        Assert.Empty(documents.Items);
        Assert.Equal("PendingReview", reviews.Review!.Status);
    }

    [Fact]
    public async Task Deactivated_service_blocks_apply_and_keeps_review_pending()
    {
        var (service, apply, documents, reviews, accountId, userId) = await Scenario();
        service.IsActive = false;

        var result = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(documents.Items);
        Assert.Equal("PendingReview", reviews.Review!.Status);
    }

    [Fact]
    public async Task Unknown_or_unselected_items_are_rejected()
    {
        var (_, apply, documents, reviews, accountId, userId) = await Scenario();

        var result = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(Guid.NewGuid(), 1m)], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(documents.Items);
    }

    [Fact]
    public async Task Invalid_quantity_is_rejected_without_creating_documents()
    {
        var (service, apply, documents, reviews, accountId, userId) = await Scenario();

        var result = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, 10001m)], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(documents.Items);
        Assert.Equal("PendingReview", reviews.Review!.Status);
    }

    [Fact]
    public async Task Review_from_another_account_is_not_found()
    {
        var (service, apply, documents, reviews, _, _) = await Scenario();

        var result = await apply.ApplyAsync(Guid.NewGuid(), Guid.NewGuid(), reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(documents.Items);
    }

    [Fact]
    public async Task Simultaneous_applies_result_in_a_single_document_for_the_loser_via_replay()
    {
        var (service, apply, documents, _, reviews, accountId, userId) = await Scenario();
        var winner = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);
        Assert.True(winner.Succeeded);

        // Simula um segundo fluxo que leu a revisão como pendente antes do commit do vencedor.
        // O fake de UoW não desfaz inserts; em produção o rollback descarta o rascunho do perdedor.
        // O que se afirma aqui: o perdedor recebe o documento do vencedor e a revisão aponta para ele.
        reviews.SimulateLostRace(winner.DocumentId!.Value);

        var loser = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);

        Assert.True(loser.Succeeded);
        Assert.Equal(winner.DocumentId, loser.DocumentId);
        Assert.Equal(winner.DocumentId, reviews.Review!.AppliedDocumentId);
        Assert.Equal(1, reviews.SuccessfulClaims);
    }

    [Fact]
    public async Task Failed_claim_with_different_content_conflicts()
    {
        var (service, apply, _, reviews, accountId, userId) = await Scenario();
        reviews.SimulateLostRace(Guid.NewGuid(), winnerFingerprint: "outro-conteudo");

        var result = await apply.ApplyAsync(userId, accountId, reviews.Review!.Id, [new(service.Id, null)], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.Conflict);
    }

    [Fact]
    public async Task Dismissed_review_cannot_be_applied()
    {
        var (service, apply, documents, reviews, accountId, userId) = await Scenario();
        await reviews.MarkAsync(accountId, reviews.Review!.Id, "Dismissed", CancellationToken.None);

        var result = await apply.ApplyAsync(userId, accountId, reviews.Review.Id, [new(service.Id, null)], CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Empty(documents.Items);
    }

    private static async Task<(ServiceCatalogItem Service, BudgetSuggestionApplyService Apply, ListRepository<Document> Documents,
        FakeReviewService Reviews, Guid AccountId, Guid UserId)> Scenario(decimal itemQuantity = 1m)
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new ServiceCatalogItem { AccountId = accountId, Name = "Instalação", UnitCode = "serviço", StandardPrice = 100, IsActive = true };
        var services = new ListRepository<ServiceCatalogItem>();
        await services.AddAsync(service);

        var documents = new ListRepository<Document>();
        var items = new ListRepository<DocumentItem>();
        var uow = new FakeUnitOfWork();
        var wizard = new BudgetWizardService(documents, items, new ListRepository<Client>(), services,
            new ListRepository<BudgetTemplate>(), new ListRepository<BudgetTemplateItem>(), new ListRepository<AccountSettings>(),
            uow, new FakeNumbers());

        var reviews = new FakeReviewService(accountId, userId, service, itemQuantity);
        var apply = new BudgetSuggestionApplyService(reviews, wizard, documents, uow);
        return (service, apply, documents, reviews, accountId, userId);
    }

    private sealed class FakeReviewService : IAiSuggestionReviewService
    {
        public FakeReviewService(Guid accountId, Guid userId, ServiceCatalogItem service, decimal quantity)
        {
            Review = new AiBudgetSuggestionReview(Guid.NewGuid(), accountId, "PendingReview", "Escopo", "Observações", "Aviso", false,
                [new AiBudgetSuggestionItem(service.Id, service.Name, quantity, service.StandardPrice, service.UnitCode)]);
        }

        public AiBudgetSuggestionReview? Review { get; private set; }
        public int SuccessfulClaims { get; private set; }

        private bool _loseClaim;
        private Guid _winnerDocumentId;
        private string? _winnerFingerprint;

        /// <summary>Reabre a revisão como pendente e faz a próxima reivindicação falhar,
        /// como se outro fluxo tivesse aplicado primeiro (vencendo a corrida).</summary>
        public void SimulateLostRace(Guid winnerDocumentId, string? winnerFingerprint = null)
        {
            _loseClaim = true;
            _winnerDocumentId = winnerDocumentId;
            _winnerFingerprint = winnerFingerprint;
            Review = Review! with { Status = "PendingReview", AppliedDocumentId = null, ApplyFingerprint = null };
        }

        public Task<Guid> SavePendingAsync(Guid accountId, Guid userId, BudgetAiSuggestionResult result, CancellationToken ct = default)
            => Task.FromResult(Guid.NewGuid());

        public Task<AiBudgetSuggestionReview?> FindAsync(Guid accountId, Guid id, CancellationToken ct = default)
            => Task.FromResult(Review is not null && Review.AccountId == accountId && Review.Id == id ? Review : null);

        public Task<IReadOnlyList<AiBudgetSuggestionReview>> ListPendingAsync(Guid accountId, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AiBudgetSuggestionReview>>(Review is { Status: "PendingReview" } r && r.AccountId == accountId ? [r] : []);

        public Task<bool> MarkAsync(Guid accountId, Guid id, string status, CancellationToken ct = default)
        {
            if (Review is null || Review.AccountId != accountId || Review.Id != id || Review.Status != "PendingReview")
                return Task.FromResult(false);
            Review = Review with { Status = status };
            return Task.FromResult(true);
        }

        public Task<bool> TryMarkAppliedAsync(Guid accountId, Guid id, string applyFingerprint, Guid documentId, CancellationToken ct = default)
        {
            if (Review is null || Review.AccountId != accountId || Review.Id != id || Review.Status != "PendingReview")
                return Task.FromResult(false);
            if (_loseClaim)
            {
                _loseClaim = false;
                // Simula o vencedor da corrida: a revisão já consta aplicada.
                Review = Review with
                {
                    Status = "Applied",
                    ApplyFingerprint = _winnerFingerprint ?? applyFingerprint,
                    AppliedDocumentId = _winnerDocumentId
                };
                return Task.FromResult(false);
            }
            SuccessfulClaims++;
            Review = Review with { Status = "Applied", ApplyFingerprint = applyFingerprint, AppliedDocumentId = documentId };
            return Task.FromResult(true);
        }
    }

    private sealed class FakeNumbers : IDocumentNumberService
    {
        private int _value;
        public Task<string> NextAsync(Guid userId, DocumentType type, CancellationToken ct = default) => Task.FromResult($"ORC-{++_value}");
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private bool _active;
        public bool HasActiveTransaction => _active;
        public Task BeginTransactionAsync(CancellationToken ct = default) { _active = true; return Task.CompletedTask; }
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task CommitTransactionAsync(CancellationToken ct = default) { _active = false; return Task.CompletedTask; }
        public Task RollbackTransactionAsync(CancellationToken ct = default) { _active = false; return Task.CompletedTask; }
    }

    private sealed class ListRepository<T> : IRepository<T> where T : class
    {
        public List<T> Items { get; } = [];
        public Task<T?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult<T?>(null);
        public Task AddAsync(T entity, CancellationToken ct = default) { Items.Add(entity); return Task.CompletedTask; }
        public void Remove(T entity) => Items.Remove(entity);
        public IQueryable<T> Query() => Items.AsQueryable();
    }
}
