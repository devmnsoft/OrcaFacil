using System.Text.Json;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Documents;

public sealed record BudgetWizardItem(Guid? ServiceCatalogItemId, string Description, string Unit, decimal Quantity, decimal UnitPrice, decimal Discount, string? Notes, int SortOrder);
public sealed record BudgetWizardViewModel(Guid DocumentId, Guid? ClientId, string ClientName, int CurrentStep, DateTime? ValidUntil,
    DateTime? ExpectedStartAt, string? EstimatedDuration, string? PaymentMethod, int? InstallmentCount, decimal? DepositAmount,
    string? PixInformation, string? WarrantyText, string? ConditionsText, string TemplateCode, decimal Discount,
    IReadOnlyList<BudgetWizardItem> Items, string RowVersion, DateTime? LastAutosavedAt);
public sealed record SaveBudgetDraftRequest(Guid DocumentId, Guid? ClientId, int CurrentStep, DateTime? ValidUntil,
    DateTime? ExpectedStartAt, string? EstimatedDuration, string? PaymentMethod, int? InstallmentCount, decimal? DepositAmount,
    string? PixInformation, string? WarrantyText, string? ConditionsText, string TemplateCode, decimal Discount,
    IReadOnlyList<BudgetWizardItem> Items, string RowVersion, string IdempotencyKey);
public sealed record BudgetDraftResult(bool Succeeded, string? Error, BudgetWizardViewModel? Draft = null, bool Conflict = false);

public sealed class BudgetWizardService
{
    private static readonly HashSet<string> Templates = new(StringComparer.OrdinalIgnoreCase) { "essential", "professional", "business" };
    private readonly IRepository<Document> _documents;
    private readonly IRepository<DocumentItem> _items;
    private readonly IRepository<Client> _clients;
    private readonly IRepository<ServiceCatalogItem> _services;
    private readonly IRepository<BudgetTemplate> _templates;
    private readonly IRepository<BudgetTemplateItem> _templateItems;
    private readonly IRepository<AccountSettings> _accountSettings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentNumberService _numbers;

    public BudgetWizardService(IRepository<Document> documents, IRepository<DocumentItem> items, IRepository<Client> clients, IRepository<ServiceCatalogItem> services,
        IRepository<BudgetTemplate> templates, IRepository<BudgetTemplateItem> templateItems, IRepository<AccountSettings> accountSettings,
        IUnitOfWork unitOfWork, IDocumentNumberService numbers)
    { _documents = documents; _items = items; _clients = clients; _services = services; _templates = templates; _templateItems = templateItems; _accountSettings = accountSettings; _unitOfWork = unitOfWork; _numbers = numbers; }

    public async Task<BudgetWizardViewModel> OpenAsync(Guid userId, Guid? accountId, Guid? documentId, Guid? clientId, CancellationToken ct,
        IReadOnlyCollection<Guid>? serviceIds = null, Guid? templateId = null)
    {
        var document = documentId is null ? null : _documents.Query().SingleOrDefault(x => x.Id == documentId && x.AccountId == accountId && !x.IsDeleted && (accountId != null || x.UserId == userId));
        if (document is null)
        {
            document = new Document { UserId = userId, AccountId = accountId, Type = DocumentType.Budget, Status = "Draft", CurrentWizardStep = 0 };
            var defaults = accountId.HasValue ? _accountSettings.Query().SingleOrDefault(x => x.AccountId == accountId && !x.IsDeleted) : null;
            if (defaults is not null)
            {
                document.ValidUntil = DateTime.UtcNow.Date.AddDays(Math.Clamp(defaults.DefaultQuoteValidityDays, 1, 365));
                document.EstimatedDuration = defaults.DefaultDeliveryTerm;
                document.ConditionsText = defaults.DefaultCommercialTerms;
                document.PixInformation = defaults.ShowBankDetails ? defaults.PixKey : null;
            }
            document.IssueNumber(await _numbers.NextAsync(userId, DocumentType.Budget, ct));
            if (clientId.HasValue) ApplyClient(document, FindClient(userId, accountId, clientId.Value));
            await _documents.AddAsync(document, ct);
            foreach (var serviceId in (serviceIds ?? []).Distinct().Take(30))
            {
                var service = _services.Query().SingleOrDefault(x => x.Id == serviceId && x.AccountId == accountId && x.IsActive && !x.IsDeleted);
                if (service is not null) await _items.AddAsync(ToDocumentItem(document.Id, service), ct);
            }
            if (templateId.HasValue)
            {
                await InitializeFromTemplateAsync(document, userId, accountId, templateId.Value, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
        }
        return Map(document);
    }

    private async Task InitializeFromTemplateAsync(Document document, Guid userId, Guid? accountId, Guid templateId, CancellationToken ct)
    {
        var template = _templates.Query().SingleOrDefault(x => x.Id == templateId && x.IsActive && !x.IsDeleted &&
            (x.IsSystemTemplate || (x.AccountId == accountId && x.UserId == userId)));
        if (template is null) return;
        var templateItems = _templateItems.Query().Where(x => x.BudgetTemplateId == template.Id && !x.IsDeleted).OrderBy(x => x.SortOrder).Take(100).ToList();
        foreach (var item in templateItems)
            await _items.AddAsync(new DocumentItem { DocumentId = document.Id, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity, UnitPrice = item.UnitPrice, SortOrder = item.SortOrder }, ct);
        document.TemplateSnapshot = JsonSerializer.Serialize(new { template.Id, template.Title, CopiedAt = DateTime.UtcNow });
    }

    private static DocumentItem ToDocumentItem(Guid documentId, ServiceCatalogItem service) => new()
    {
        DocumentId = documentId, ServiceCatalogItemId = service.Id, Description = service.Description ?? service.Name,
        Unit = service.UnitCode, Quantity = 1, UnitPrice = service.StandardPrice,
        EstimatedCostSnapshot = service.EstimatedCost, DurationMinutesSnapshot = service.SuggestedDurationMinutes
    };

    public async Task<BudgetDraftResult> SaveAsync(Guid userId, Guid? accountId, SaveBudgetDraftRequest request, CancellationToken ct)
    {
        var invalid = ValidateCommercial(request, strict: false);
        if (invalid is not null) return new(false, invalid, DraftFrom(request));
        var document = _documents.Query().SingleOrDefault(x => x.Id == request.DocumentId && x.AccountId == accountId && x.Status == "Draft" && !x.IsDeleted && (accountId != null || x.UserId == userId));
        if (document is null) return new(false, "Rascunho não encontrado nesta conta.");
        if (document.LastAutosaveKey == request.IdempotencyKey) return new(true, null, Map(document));
        if (!Convert.ToBase64String(document.RowVersion).Equals(request.RowVersion, StringComparison.Ordinal))
            return new(false, "Este rascunho foi atualizado em outra janela. Recarregue para continuar.", Map(document), true);
        if (!Templates.Contains(request.TemplateCode)) return new(false, "Modelo de apresentação inválido.");

        var selectedClient = request.ClientId.HasValue ? FindClient(userId, accountId, request.ClientId.Value) : null;
        if (request.ClientId.HasValue && selectedClient is null) return new(false, "O cliente selecionado não pertence à conta atual.");
        ApplyClient(document, selectedClient);
        document.CurrentWizardStep = Math.Clamp(request.CurrentStep, 0, 4);
        document.ValidUntil = request.ValidUntil;
        document.ExpectedStartAt = request.ExpectedStartAt;
        document.EstimatedDuration = Clean(request.EstimatedDuration, 120);
        document.PaymentMethod = Clean(request.PaymentMethod, 60);
        document.InstallmentCount = request.InstallmentCount is > 0 and <= 24 ? request.InstallmentCount : null;
        document.DepositAmount = request.DepositAmount is >= 0 ? request.DepositAmount : null;
        document.PixInformation = Clean(request.PixInformation, 300);
        document.WarrantyText = Clean(request.WarrantyText, 2000);
        document.ConditionsText = Clean(request.ConditionsText, 4000);
        document.TemplateCode = request.TemplateCode.ToLowerInvariant();
        document.TemplateSnapshot = JsonSerializer.Serialize(new { Code = document.TemplateCode, SavedAt = DateTime.UtcNow });
        document.Discount = CommercialCalculator.Round(request.Discount);
        foreach (var old in _items.Query().Where(x => x.DocumentId == document.Id).ToList()) _items.Remove(old);
        document.Items = request.Items.Where(x => !string.IsNullOrWhiteSpace(x.Description)).Take(100).Select((x, index) =>
        {
            var service = x.ServiceCatalogItemId.HasValue ? _services.Query().SingleOrDefault(s => s.Id == x.ServiceCatalogItemId && s.AccountId == accountId && s.IsActive && !s.IsDeleted) : null;
            return new DocumentItem { DocumentId = document.Id, ServiceCatalogItemId = service?.Id, Description = service?.Description ?? x.Description.Trim(), Unit = service?.UnitCode ?? Clean(x.Unit, 40) ?? "serviço", Quantity = x.Quantity, UnitPrice = service?.StandardPrice ?? CommercialCalculator.Round(x.UnitPrice), EstimatedCostSnapshot = service?.EstimatedCost ?? 0, DurationMinutesSnapshot = service?.SuggestedDurationMinutes, Discount = CommercialCalculator.Round(x.Discount), Notes = Clean(x.Notes, 1000), SortOrder = index };
        }).ToList();
        document.CalculateTotals();
        document.LastAutosavedAt = DateTime.UtcNow;
        document.LastAutosaveKey = Clean(request.IdempotencyKey, 80);
        document.RowVersion = Guid.NewGuid().ToByteArray();
        document.Touch();
        await _unitOfWork.SaveChangesAsync(ct);
        return new(true, null, Map(document));
    }

    public async Task<BudgetDraftResult> FinalizeAsync(Guid userId, Guid? accountId, SaveBudgetDraftRequest request, CancellationToken ct)
    {
        var invalid = ValidateCommercial(request, strict: true);
        if (invalid is not null) return new(false, invalid, DraftFrom(request));
        var saved = await SaveAsync(userId, accountId, request, ct);
        if (!saved.Succeeded) return saved;
        var document = _documents.Query().Single(x => x.Id == request.DocumentId && x.AccountId == accountId && (accountId != null || x.UserId == userId));
        if (document.ClientId is null) return new(false, "Selecione um cliente cadastrado antes de finalizar.", saved.Draft);
        if (document.Items.Count == 0 || document.Items.Any(x => x.Quantity <= 0)) return new(false, "Inclua ao menos um item com quantidade válida.", saved.Draft);
        if (document.ValidUntil is null || document.ValidUntil.Value.Date < DateTime.UtcNow.Date) return new(false, "Informe uma validade futura para a proposta.", saved.Draft);
        document.Status = "Ready";
        document.RowVersion = Guid.NewGuid().ToByteArray();
        document.Touch();
        await _unitOfWork.SaveChangesAsync(ct);
        return new(true, null, Map(document));
    }

    private Client? FindClient(Guid userId, Guid? accountId, Guid id) => _clients.Query().SingleOrDefault(x => x.Id == id && x.AccountId == accountId && !x.IsDeleted && (accountId != null || x.UserId == userId));
    private static string? ValidateCommercial(SaveBudgetDraftRequest request, bool strict)
    {
        if (request.Discount < 0) return "O desconto não pode ser negativo.";
        var lines = request.Items.Where(x => !string.IsNullOrWhiteSpace(x.Description)).ToArray();
        if (lines.Length == 0) return strict ? "Inclua ao menos um serviço." : null;
        if (lines.Any(x => x.Quantity < 0 || x.UnitPrice < 0 || x.Discount < 0))
            return "Quantidade, preço e desconto não podem ser negativos.";
        if (strict && lines.Any(x => x.Quantity <= 0)) return "A quantidade de cada item deve ser maior que zero.";
        var priced = lines.Where(x => x.Quantity > 0).Select(x => new CommercialLine(x.Quantity, x.UnitPrice, x.Discount)).ToArray();
        if (priced.Length == 0) return strict ? "Inclua ao menos um item com quantidade válida." : null;
        try
        {
            var totals = CommercialCalculator.Calculate(priced);
            if (CommercialCalculator.Round(request.Discount) > totals.Total)
                return "O desconto não pode superar o valor dos itens.";
        }
        catch (ArgumentException ex) { return ex.Message; }
        return null;
    }
    private static BudgetWizardViewModel DraftFrom(SaveBudgetDraftRequest request) => new(request.DocumentId, request.ClientId, "", request.CurrentStep,
        request.ValidUntil, request.ExpectedStartAt, request.EstimatedDuration, request.PaymentMethod, request.InstallmentCount, request.DepositAmount,
        request.PixInformation, request.WarrantyText, request.ConditionsText, request.TemplateCode, request.Discount, request.Items, request.RowVersion, null);
    private static void ApplyClient(Document document, Client? client)
    {
        if (client is null) { document.ClientId = null; document.ClientName = ""; document.ClientSnapshot = null; return; }
        document.ClientId = client.Id; document.ClientName = client.Name; document.ClientDocument = client.DocumentNumber; document.ClientPhone = client.Phone;
        document.ClientEmail = client.Email; document.ClientCity = client.City;
        document.ClientSnapshot = JsonSerializer.Serialize(new { client.Id, client.Name, client.DocumentNumber, client.Phone, client.Email, client.City, client.Address });
    }
    private BudgetWizardViewModel Map(Document d) => new(d.Id, d.ClientId, d.ClientName, d.CurrentWizardStep, d.ValidUntil, d.ExpectedStartAt,
        d.EstimatedDuration, d.PaymentMethod, d.InstallmentCount, d.DepositAmount, d.PixInformation, d.WarrantyText, d.ConditionsText,
        d.TemplateCode, d.Discount, _items.Query().Where(x => x.DocumentId == d.Id).OrderBy(x => x.SortOrder).Select(x =>
            new BudgetWizardItem(x.ServiceCatalogItemId, x.Description, x.Unit, x.Quantity, x.UnitPrice, x.Discount, x.Notes, x.SortOrder)).ToList(), Convert.ToBase64String(d.RowVersion), d.LastAutosavedAt);
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}

public sealed class BudgetDraftService(BudgetWizardService wizard)
{
    public Task<BudgetDraftResult> SaveAsync(Guid userId, Guid? accountId, SaveBudgetDraftRequest request, CancellationToken ct) => wizard.SaveAsync(userId, accountId, request, ct);
}
