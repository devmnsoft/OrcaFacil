using OrcaFacil.Application.Abstractions;
using Xunit;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.UnitTests;

public sealed class BudgetWizardIntegrityTests
{
    [Fact]
    public async Task Invalid_document_does_not_create_a_draft()
    {
        var documents = new ListRepository<Document>();
        var wizard = Wizard(documents, new ListRepository<DocumentItem>(), new ListRepository<ServiceCatalogItem>());
        var result = await wizard.OpenAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Empty(documents.Items);
    }

    [Fact]
    public async Task Same_key_and_same_content_returns_the_previous_draft()
    {
        var (wizard, draft, userId, accountId) = await DraftAsync();
        var first = await wizard.SaveAsync(userId, accountId, Request(draft, "mesma-chave", "Serviço"), CancellationToken.None);
        var second = await wizard.SaveAsync(userId, accountId, Request(draft, "mesma-chave", "Serviço", first.Draft!.RowVersion), CancellationToken.None);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Draft!.DocumentId, second.Draft!.DocumentId);
        Assert.Equal(first.Draft.RowVersion, second.Draft.RowVersion);
        Assert.Single(second.Draft.Items);
    }

    [Fact]
    public async Task Same_key_and_different_content_conflicts()
    {
        var (wizard, draft, userId, accountId) = await DraftAsync();
        await wizard.SaveAsync(userId, accountId, Request(draft, "mesma-chave", "Serviço"), CancellationToken.None);
        var second = await wizard.SaveAsync(userId, accountId, Request(draft, "mesma-chave", "Outro serviço"), CancellationToken.None);
        Assert.True(second.Conflict);
        Assert.False(second.Succeeded);
    }

    [Fact]
    public async Task Stale_version_conflicts_before_overwriting()
    {
        var (wizard, draft, userId, accountId) = await DraftAsync();
        var first = await wizard.SaveAsync(userId, accountId, Request(draft, "chave-1", "Serviço"), CancellationToken.None);
        var stale = await wizard.SaveAsync(userId, accountId, Request(draft, "chave-2", "Alterado", draft.RowVersion), CancellationToken.None);
        Assert.True(stale.Conflict);
        Assert.Equal("Serviço", first.Draft!.Items[0].Description);
    }

    [Fact]
    public async Task Unknown_service_is_rejected_and_not_converted_to_a_loose_item()
    {
        var (wizard, draft, userId, accountId) = await DraftAsync();
        var request = Request(draft, "chave", "Avulso") with { Items = [new BudgetWizardItem(Guid.NewGuid(), "Avulso", "serviço", 1, 10, 0, null, 0)] };
        var result = await wizard.SaveAsync(userId, accountId, request, CancellationToken.None);
        Assert.False(result.Succeeded);
        Assert.Contains("não pertence", result.Error);
        var reopened = await wizard.OpenAsync(userId, accountId, draft.DocumentId, null, CancellationToken.None);
        Assert.Empty(reopened.Draft!.Items);
    }

    [Fact]
    public async Task Save_keeps_the_contracted_price_when_the_catalog_changes()
    {
        var services = new ListRepository<ServiceCatalogItem>();
        var service = new ServiceCatalogItem { AccountId = Guid.NewGuid(), Name = "Instalação", UnitCode = "serviço", StandardPrice = 100, IsActive = true };
        await services.AddAsync(service);
        var (wizard, draft, userId, _) = await DraftAsync(services, service.AccountId);
        var first = await wizard.SaveAsync(userId, service.AccountId, Request(draft, "chave-preco", "Instalação", serviceId: service.Id, price: 100), CancellationToken.None);
        service.StandardPrice = 180;
        var second = await wizard.SaveAsync(userId, service.AccountId, Request(draft, "chave-preco-2", "Instalação", first.Draft!.RowVersion, service.Id, 100), CancellationToken.None);
        Assert.True(second.Succeeded);
        Assert.Equal(100, second.Draft!.Items[0].UnitPrice);
        Assert.Contains(second.Notices!, x => x.Contains("mantém o preço contratado"));
    }

    [Fact]
    public async Task Repeated_catalog_apply_does_not_duplicate_items()
    {
        var services = new ListRepository<ServiceCatalogItem>();
        var service = new ServiceCatalogItem { AccountId = Guid.NewGuid(), Name = "Visita", UnitCode = "serviço", StandardPrice = 50, IsActive = true };
        await services.AddAsync(service);
        var (wizard, draft, userId, _) = await DraftAsync(services, service.AccountId);
        var first = await wizard.ApplyCatalogItemsAsync(userId, service.AccountId, draft.DocumentId, draft.RowVersion, "aplicar-1", [service.Id], CancellationToken.None);
        var second = await wizard.ApplyCatalogItemsAsync(userId, service.AccountId, draft.DocumentId, first.Draft!.RowVersion, "aplicar-1", [service.Id], CancellationToken.None);
        Assert.True(second.Succeeded);
        Assert.Single(second.Draft!.Items);
    }

    [Fact]
    public void Empty_or_foreign_ai_payload_is_not_accepted()
    {
        var catalogId = Guid.NewGuid();
        var catalog = new Dictionary<Guid, ServiceCatalogItem> { [catalogId] = new() { Name = "Visita", StandardPrice = 10, UnitCode = "serviço" } };
        Assert.False(BudgetSuggestionParser.Parse("", catalog).Accepted);
        Assert.False(BudgetSuggestionParser.Parse("não é json", catalog).Accepted);
        var foreign = BudgetSuggestionParser.Parse($"{{\"scope\":\"Escopo\",\"notes\":\"\",\"items\":[{{\"catalogItemId\":\"{Guid.NewGuid()}\",\"quantity\":1,\"price\":999}}]}}", catalog);
        Assert.True(foreign.Accepted);
        Assert.Empty(foreign.Items);
        var matched = BudgetSuggestionParser.Parse($"{{\"scope\":\"Escopo\",\"notes\":\"Obs\",\"items\":[{{\"catalogItemId\":\"{catalogId}\",\"quantity\":2}}]}}", catalog);
        Assert.Equal(catalogId, matched.Items[0].CatalogItemId);
        Assert.Equal(2, matched.Items[0].Quantity);
    }

    private static async Task<(BudgetWizardService Wizard, BudgetWizardViewModel Draft, Guid UserId, Guid AccountId)> DraftAsync(ListRepository<ServiceCatalogItem>? services = null, Guid? accountId = null)
    {
        services ??= new ListRepository<ServiceCatalogItem>();
        var documents = new ListRepository<Document>();
        var wizard = Wizard(documents, new ListRepository<DocumentItem>(), services);
        var userId = Guid.NewGuid();
        var resolvedAccount = accountId ?? Guid.NewGuid();
        var opened = await wizard.OpenAsync(userId, resolvedAccount, null, null, CancellationToken.None);
        return (wizard, opened.Draft!, userId, resolvedAccount);
    }

    private static BudgetWizardService Wizard(ListRepository<Document> documents, ListRepository<DocumentItem> items, ListRepository<ServiceCatalogItem> services) =>
        new(documents, items, new ListRepository<Client>(), services, new ListRepository<BudgetTemplate>(), new ListRepository<BudgetTemplateItem>(), new ListRepository<AccountSettings>(), new FakeUnitOfWork(), new FakeNumbers());

    private static SaveBudgetDraftRequest Request(BudgetWizardViewModel draft, string key, string description, string? rowVersion = null, Guid? serviceId = null, decimal price = 25) =>
        new(draft.DocumentId, null, 1, DateTime.UtcNow.Date.AddDays(5), null, null, "Pix", 1, 0, null, null, null, "essential", 0,
            [new BudgetWizardItem(serviceId, description, "serviço", 1, price, 0, null, 0)], rowVersion ?? draft.RowVersion, key);

    private sealed class FakeNumbers : IDocumentNumberService
    {
        private int _value;
        public Task<string> NextAsync(Guid userId, DocumentType type, CancellationToken ct = default) => Task.FromResult($"ORC-{++_value}");
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task CommitTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
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
