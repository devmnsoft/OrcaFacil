using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    IReadOnlyList<BudgetWizardItem> Items, string RowVersion, DateTime? LastAutosavedAt, string? Number = null);
public sealed record SaveBudgetDraftRequest(Guid DocumentId, Guid? ClientId, int CurrentStep, DateTime? ValidUntil,
    DateTime? ExpectedStartAt, string? EstimatedDuration, string? PaymentMethod, int? InstallmentCount, decimal? DepositAmount,
    string? PixInformation, string? WarrantyText, string? ConditionsText, string TemplateCode, decimal Discount,
    IReadOnlyList<BudgetWizardItem> Items, string RowVersion, string IdempotencyKey, bool ConfirmPriceChanges = false);
public sealed record BudgetDraftResult(bool Succeeded, string? Error, BudgetWizardViewModel? Draft = null, bool Conflict = false, IReadOnlyList<string>? Notices = null);
public sealed record BudgetOpenResult(bool Succeeded, string? Error, BudgetWizardViewModel? Draft, bool Conflict = false);

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

    public sealed record DraftServiceSeed(ServiceCatalogItem Service, decimal Quantity);

    public async Task<BudgetOpenResult> OpenAsync(Guid userId, Guid? accountId, Guid? documentId, Guid? clientId, CancellationToken ct,
        IReadOnlyCollection<Guid>? serviceIds = null, Guid? templateId = null, string? idempotencyKey = null)
    {
        if (documentId.HasValue)
        {
            var existing = FindDocument(userId, accountId, documentId.Value);
            if (existing is null)
                return new(false, "Orçamento não encontrado ou sem acesso nesta conta.", null);
            return new(true, null, Map(existing));
        }

        var keyError = ValidateKey(idempotencyKey, required: false);
        if (keyError is not null) return new(false, keyError, null);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var matches = FindByKey(userId, accountId, idempotencyKey.Trim()).ToList();
            if (matches.Count > 1) return new(false, "Esta chave de criação está ambígua. Recarregue a lista de rascunhos.", null, true);
            if (matches.Count == 1)
            {
                var prior = matches[0];
                if (CreationMatches(prior, clientId, serviceIds, templateId)) return new(true, null, Map(prior));
                return new(false, "Esta chave de criação já foi usada com outro conteúdo.", null, true);
            }
        }

        var requestedServices = (serviceIds ?? []).Where(x => x != Guid.Empty).Distinct().ToArray();
        if (requestedServices.Length > 30)
            return new(false, "A criação aceita no máximo 30 serviços de catálogo.", null);
        var resolved = new List<DraftServiceSeed>();
        foreach (var serviceId in requestedServices)
        {
            var service = FindAvailableService(accountId, serviceId);
            if (service is null)
                return new(false, "O serviço informado não pertence à conta ou está indisponível.", null);
            resolved.Add(new(service, 1m));
        }

        List<BudgetTemplateItem> templateItems = [];
        if (templateId.HasValue)
        {
            var template = FindTemplate(userId, accountId, templateId.Value);
            if (template is null) return new(false, "O modelo informado não está disponível nesta conta.", null);
            templateItems = _templateItems.Query().Where(x => x.BudgetTemplateId == template.Id && !x.IsDeleted).OrderBy(x => x.SortOrder).ToList();
            if (templateItems.Count > 100)
                return new(false, "O modelo tem mais de 100 itens e não foi copiado.", null);
        }

        return await CreateDraftCoreAsync(userId, accountId, clientId, resolved, templateId, templateItems,
            idempotencyKey, suggestionText: null, ct);
    }

    /// <summary>
    /// Cria o rascunho a partir de uma sugestão de IA revisada. Não gerencia transação:
    /// o chamador deve abrir a transação ambiente para que documento e revisão sejam
    /// persistidos atomicamente. A recuperação de repetição fica a cargo do chamador.
    /// </summary>
    public async Task<BudgetOpenResult> CreateSuggestionDraftAsync(Guid userId, Guid accountId,
        IReadOnlyList<DraftServiceSeed> services, string? suggestionText, string idempotencyKey, CancellationToken ct)
    {
        var keyError = ValidateKey(idempotencyKey, required: true);
        if (keyError is not null) return new(false, keyError, null);
        if (services.Count == 0 || services.Count > 30)
            return new(false, "A aplicação da sugestão aceita entre 1 e 30 itens do catálogo.", null);
        var revalidated = new List<DraftServiceSeed>(services.Count);
        foreach (var seed in services)
        {
            var service = FindAvailableService(accountId, seed.Service.Id);
            if (service is null)
                return new(false, "O serviço informado não pertence à conta ou está indisponível.", null);
            if (seed.Quantity <= 0 || seed.Quantity > 10000)
                return new(false, "A quantidade de um item da sugestão é inválida.", null);
            revalidated.Add(new(service, seed.Quantity));
        }
        return await CreateDraftCoreAsync(userId, accountId, null, revalidated, null, [], idempotencyKey, suggestionText, ct);
    }

    private async Task<BudgetOpenResult> CreateDraftCoreAsync(Guid userId, Guid? accountId, Guid? clientId,
        IReadOnlyList<DraftServiceSeed> resolved, Guid? templateId, IReadOnlyList<BudgetTemplateItem> templateItems,
        string? idempotencyKey, string? suggestionText, CancellationToken ct)
    {
        var ownsTransaction = !_unitOfWork.HasActiveTransaction;
        var document = new Document { UserId = userId, AccountId = accountId, Type = DocumentType.Budget, Status = "Draft", CurrentWizardStep = 0 };
        var defaults = accountId.HasValue ? _accountSettings.Query().SingleOrDefault(x => x.AccountId == accountId && !x.IsDeleted) : null;
        if (defaults is not null)
        {
            document.ValidUntil = DateTime.UtcNow.Date.AddDays(Math.Clamp(defaults.DefaultQuoteValidityDays, 1, 365));
            document.EstimatedDuration = defaults.DefaultDeliveryTerm;
            document.ConditionsText = defaults.DefaultCommercialTerms;
            document.PixInformation = defaults.ShowBankDetails ? defaults.PixKey : null;
        }
        if (!string.IsNullOrWhiteSpace(suggestionText))
            document.Notes = suggestionText.Length > 4000 ? suggestionText[..4000] : suggestionText;
        if (clientId.HasValue)
        {
            var client = FindClient(userId, accountId, clientId.Value);
            if (client is null) return new(false, "O cliente selecionado não pertence à conta atual.", null);
            ApplyClient(document, client);
        }
        if (!string.IsNullOrWhiteSpace(idempotencyKey)) document.LastAutosaveKey = idempotencyKey.Trim();
        try
        {
            if (ownsTransaction) await _unitOfWork.BeginTransactionAsync(ct);
            document.IssueNumber(await _numbers.NextAsync(userId, DocumentType.Budget, accountId, ct));
            await _documents.AddAsync(document, ct);
            foreach (var seed in resolved)
                await _items.AddAsync(ToDocumentItem(document.Id, seed.Service, seed.Quantity), ct);
            foreach (var item in templateItems)
                await _items.AddAsync(new DocumentItem { DocumentId = document.Id, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity, UnitPrice = item.UnitPrice, SortOrder = item.SortOrder }, ct);
            if (templateId.HasValue)
                document.TemplateSnapshot = JsonSerializer.Serialize(new { Id = templateId, CopiedAt = DateTime.UtcNow });
            await _unitOfWork.SaveChangesAsync(ct);
            if (ownsTransaction) await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch (Exception ex) when (ownsTransaction && IsPersistenceConflict(ex))
        {
            await SafeRollbackAsync();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var recovered = FindByKey(userId, accountId, idempotencyKey.Trim()).ToList();
                if (recovered.Count == 1)
                    return new(true, null, Map(recovered[0]));
            }
            return new(false, "Não foi possível criar o rascunho sem duplicá-lo. Recarregue.", null, true);
        }
        return new(true, null, Map(document));
    }

    public ServiceCatalogItem? FindAccountService(Guid? accountId, Guid id) => FindAvailableService(accountId, id);

    private BudgetTemplate? FindTemplate(Guid userId, Guid? accountId, Guid templateId) =>
        _templates.Query().SingleOrDefault(x => x.Id == templateId && x.IsActive && !x.IsDeleted &&
            (x.IsSystemTemplate || (x.AccountId == accountId && x.UserId == userId)));

    private static DocumentItem ToDocumentItem(Guid documentId, ServiceCatalogItem service, decimal quantity = 1m) => new()
    {
        DocumentId = documentId, ServiceCatalogItemId = service.Id, Description = service.Description ?? service.Name,
        Unit = service.UnitCode, Quantity = quantity, UnitPrice = service.StandardPrice,
        EstimatedCostSnapshot = service.EstimatedCost, DurationMinutesSnapshot = service.SuggestedDurationMinutes
    };

    public async Task<BudgetDraftResult> SaveAsync(Guid userId, Guid? accountId, SaveBudgetDraftRequest request, CancellationToken ct)
    {
        var keyError = ValidateKey(request.IdempotencyKey, required: true);
        if (keyError is not null) return new(false, keyError, DraftFrom(request));
        var invalid = ValidateCommercial(request, strict: false);
        if (invalid is not null) return new(false, invalid, DraftFrom(request));
        var lengthError = ValidateLengths(request);
        if (lengthError is not null) return new(false, lengthError, DraftFrom(request));
        var document = FindDraft(userId, accountId, request.DocumentId);
        if (document is null) return new(false, "Rascunho não encontrado nesta conta.");
        var meaningful = request.Items.Where(x => !string.IsNullOrWhiteSpace(x.Description)).ToList();
        if (meaningful.Count > 100) return new(false, "O orçamento aceita no máximo 100 itens. Remova itens antes de salvar.", DraftFrom(request));
        var fingerprint = Fingerprint(request, meaningful);
        if (string.Equals(document.LastAutosaveKey, request.IdempotencyKey.Trim(), StringComparison.Ordinal))
        {
            if (StoredFingerprint(document) == fingerprint) return new(true, null, Map(document));
            return new(false, "Esta chave de salvamento já foi usada com outro conteúdo.", Map(document), true);
        }
        if (!Convert.ToBase64String(document.RowVersion).Equals(request.RowVersion, StringComparison.Ordinal))
            return new(false, "Este rascunho foi atualizado em outra janela. Recarregue para continuar.", Map(document), true);
        if (!Templates.Contains(request.TemplateCode)) return new(false, "Modelo de apresentação inválido.", DraftFrom(request));

        var selectedClient = request.ClientId.HasValue ? FindClient(userId, accountId, request.ClientId.Value) : null;
        if (request.ClientId.HasValue && selectedClient is null) return new(false, "O cliente selecionado não pertence à conta atual.", DraftFrom(request));
        var prepared = PrepareItems(accountId, document.Id, meaningful, request.ConfirmPriceChanges);
        if (prepared.Error is not null) return new(false, prepared.Error, DraftFrom(request));

        var previousItems = _items.Query().Where(x => x.DocumentId == document.Id && !x.IsDeleted).ToList();
        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            ApplyClient(document, selectedClient);
            document.CurrentWizardStep = Math.Clamp(request.CurrentStep, 0, 4);
            document.ValidUntil = request.ValidUntil;
            document.ExpectedStartAt = request.ExpectedStartAt;
            document.EstimatedDuration = TrimOrNull(request.EstimatedDuration);
            document.PaymentMethod = TrimOrNull(request.PaymentMethod);
            document.InstallmentCount = request.InstallmentCount is > 0 and <= 24 ? request.InstallmentCount : null;
            document.DepositAmount = request.DepositAmount is >= 0 ? request.DepositAmount : null;
            document.PixInformation = TrimOrNull(request.PixInformation);
            document.WarrantyText = TrimOrNull(request.WarrantyText);
            document.ConditionsText = TrimOrNull(request.ConditionsText);
            document.TemplateCode = request.TemplateCode.Trim().ToLowerInvariant();
            document.TemplateSnapshot = JsonSerializer.Serialize(new { Code = document.TemplateCode, SavedAt = DateTime.UtcNow });
            document.Discount = CommercialCalculator.Round(request.Discount);
            foreach (var old in previousItems) _items.Remove(old);
            foreach (var item in prepared.Items) await _items.AddAsync(item, ct);
            document.Items = prepared.Items;
            document.CalculateTotals();
            document.LastAutosavedAt = DateTime.UtcNow;
            document.LastAutosaveKey = request.IdempotencyKey.Trim();
            document.RowVersion = Guid.NewGuid().ToByteArray();
            document.Touch();
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch (Exception ex) when (IsPersistenceConflict(ex))
        {
            await SafeRollbackAsync();
            return new(false, "Este rascunho foi atualizado em outra janela. Recarregue para continuar.", null, true);
        }
        return new(true, null, Map(document), false, prepared.Notices);
    }

    public async Task<BudgetDraftResult> ApplyCatalogItemsAsync(Guid userId, Guid? accountId, Guid documentId, string rowVersion, string idempotencyKey, IReadOnlyCollection<Guid> catalogIds, CancellationToken ct)
    {
        var document = FindDraft(userId, accountId, documentId);
        if (document is null) return new(false, "Rascunho não encontrado nesta conta.");
        var existing = _items.Query().Where(x => x.DocumentId == document.Id && !x.IsDeleted).OrderBy(x => x.SortOrder).ToList();
        var seen = existing.Select(x => x.ServiceCatalogItemId).Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();
        var merged = existing.Select(x => new BudgetWizardItem(x.ServiceCatalogItemId, x.Description, x.Unit, x.Quantity, x.UnitPrice, x.Discount, x.Notes, x.SortOrder)).ToList();
        foreach (var catalogId in catalogIds.Where(x => x != Guid.Empty).Distinct())
        {
            if (!seen.Add(catalogId)) continue;
            var service = FindAvailableService(accountId, catalogId);
            if (service is null) return new(false, "O serviço informado não pertence à conta ou está indisponível.", Map(document));
            merged.Add(new BudgetWizardItem(service.Id, service.Description ?? service.Name, service.UnitCode, 1, service.StandardPrice, 0, null, merged.Count));
        }
        var request = new SaveBudgetDraftRequest(document.Id, document.ClientId, document.CurrentWizardStep, document.ValidUntil, document.ExpectedStartAt,
            document.EstimatedDuration, document.PaymentMethod, document.InstallmentCount, document.DepositAmount, document.PixInformation, document.WarrantyText,
            document.ConditionsText, string.IsNullOrWhiteSpace(document.TemplateCode) ? "essential" : document.TemplateCode, document.Discount, merged, rowVersion, idempotencyKey);
        return await SaveAsync(userId, accountId, request, ct);
    }

    public async Task<BudgetDraftResult> FinalizeAsync(Guid userId, Guid? accountId, SaveBudgetDraftRequest request, CancellationToken ct)
    {
        var keyError = ValidateKey(request.IdempotencyKey, required: true);
        if (keyError is not null) return new(false, keyError, DraftFrom(request));
        var invalid = ValidateCommercial(request, strict: true);
        if (invalid is not null) return new(false, invalid, DraftFrom(request));
        var current = FindDocument(userId, accountId, request.DocumentId);
        if (current is null) return new(false, "Rascunho não encontrado nesta conta.");
        if (!string.Equals(current.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            var lines = request.Items.Where(x => !string.IsNullOrWhiteSpace(x.Description)).ToList();
            if (string.Equals(current.Status, "Ready", StringComparison.OrdinalIgnoreCase)
                && string.Equals(current.LastAutosaveKey, request.IdempotencyKey.Trim(), StringComparison.Ordinal)
                && StoredFingerprint(current) == Fingerprint(request, lines))
                return new(true, null, Map(current));
            return new(false, "Este orçamento já foi finalizado.", Map(current));
        }

        var saved = await SaveAsync(userId, accountId, request, ct);
        if (!saved.Succeeded) return saved;
        var document = FindDocument(userId, accountId, request.DocumentId);
        if (document is null) return new(false, "Rascunho não encontrado nesta conta.", saved.Draft);
        var persisted = _items.Query().Where(x => x.DocumentId == document.Id && !x.IsDeleted).ToList();
        if (document.ClientId is null) return new(false, "Selecione um cliente cadastrado antes de finalizar.", saved.Draft);
        if (persisted.Count == 0 || persisted.Any(x => x.Quantity <= 0)) return new(false, "Inclua ao menos um item com quantidade válida.", saved.Draft);
        if (document.ValidUntil is null || document.ValidUntil.Value.Date < DateTime.UtcNow.Date) return new(false, "Informe uma validade futura para a proposta.", saved.Draft);
        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            document.Status = "Ready";
            document.RowVersion = Guid.NewGuid().ToByteArray();
            document.Touch();
            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch (Exception ex) when (IsPersistenceConflict(ex))
        {
            await SafeRollbackAsync();
            var again = FindDocument(userId, accountId, request.DocumentId);
            if (again is not null && string.Equals(again.Status, "Ready", StringComparison.OrdinalIgnoreCase))
                return new(true, null, Map(again));
            return new(false, "Este rascunho foi atualizado em outra janela. Recarregue para continuar.", null, true);
        }
        return new(true, null, Map(document), false, saved.Notices);
    }

    private Document? FindDocument(Guid userId, Guid? accountId, Guid id) =>
        _documents.Query().SingleOrDefault(x => x.Id == id && x.AccountId == accountId && !x.IsDeleted && x.Type == DocumentType.Budget && (accountId != null || x.UserId == userId));
    private Document? FindDraft(Guid userId, Guid? accountId, Guid id)
    {
        var document = FindDocument(userId, accountId, id);
        return document is not null && string.Equals(document.Status, "Draft", StringComparison.OrdinalIgnoreCase) ? document : null;
    }
    private IEnumerable<Document> FindByKey(Guid userId, Guid? accountId, string key) =>
        _documents.Query().Where(x => x.AccountId == accountId && x.LastAutosaveKey == key && !x.IsDeleted && x.Type == DocumentType.Budget && (accountId != null || x.UserId == userId));
    private ServiceCatalogItem? FindAvailableService(Guid? accountId, Guid id) =>
        _services.Query().SingleOrDefault(x => x.Id == id && x.AccountId == accountId && x.IsActive && !x.IsDeleted);
    private bool CreationMatches(Document document, Guid? clientId, IReadOnlyCollection<Guid>? serviceIds, Guid? templateId)
    {
        if (document.ClientId != clientId) return false;
        var expected = (serviceIds ?? []).Where(x => x != Guid.Empty).Distinct().OrderBy(x => x).ToArray();
        var actual = _items.Query().Where(x => x.DocumentId == document.Id && !x.IsDeleted && x.ServiceCatalogItemId.HasValue)
            .Select(x => x.ServiceCatalogItemId!.Value).Distinct().OrderBy(x => x).ToArray();
        if (!expected.SequenceEqual(actual)) return false;
        if (!templateId.HasValue) return document.TemplateSnapshot is null;
        return document.TemplateSnapshot?.Contains(templateId.Value.ToString(), StringComparison.OrdinalIgnoreCase) == true;
    }
    private Client? FindClient(Guid userId, Guid? accountId, Guid id) => _clients.Query().SingleOrDefault(x => x.Id == id && x.AccountId == accountId && !x.IsDeleted && (accountId != null || x.UserId == userId));
    private static string? ValidateKey(string? key, bool required)
    {
        if (string.IsNullOrWhiteSpace(key)) return required ? "Identificador de salvamento ausente." : null;
        if (key.Trim().Length > 80) return "O identificador de salvamento excede 80 caracteres.";
        return null;
    }
    private static string? ValidateLengths(SaveBudgetDraftRequest request)
    {
        var fields = new (string? Value, int Max, string Label)[]
        {
            (request.EstimatedDuration, 120, "A duração estimada"),
            (request.PaymentMethod, 60, "A forma de pagamento"),
            (request.PixInformation, 300, "A instrução Pix"),
            (request.WarrantyText, 2000, "A garantia"),
            (request.ConditionsText, 4000, "As condições"),
            (request.TemplateCode, 40, "O modelo de apresentação")
        };
        foreach (var field in fields)
            if (!string.IsNullOrWhiteSpace(field.Value) && field.Value.Trim().Length > field.Max)
                return $"{field.Label} excede {field.Max} caracteres e não foi gravada.";
        foreach (var item in request.Items.Where(x => !string.IsNullOrWhiteSpace(x.Description)))
        {
            if (item.Description.Trim().Length > 500) return "A descrição de um item excede 500 caracteres e não foi gravada.";
            if (!string.IsNullOrWhiteSpace(item.Unit) && item.Unit.Trim().Length > 40) return "A unidade de um item excede 40 caracteres e não foi gravada.";
            if (!string.IsNullOrWhiteSpace(item.Notes) && item.Notes.Trim().Length > 1000) return "A observação de um item excede 1000 caracteres e não foi gravada.";
        }
        return null;
    }
    private (string? Error, List<DocumentItem> Items, IReadOnlyList<string> Notices) PrepareItems(Guid? accountId, Guid documentId, IReadOnlyList<BudgetWizardItem> lines, bool confirmPriceChanges)
    {
        var existing = _items.Query().Where(x => x.DocumentId == documentId && !x.IsDeleted).ToList();
        var notices = new List<string>();
        var built = new List<DocumentItem>();
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            ServiceCatalogItem? service = null;
            if (line.ServiceCatalogItemId.HasValue)
            {
                service = FindAvailableService(accountId, line.ServiceCatalogItemId.Value);
                if (service is null)
                    return ("O serviço informado não pertence à conta ou está indisponível.", [], notices);
            }
            var requested = CommercialCalculator.Round(line.UnitPrice);
            var price = requested;
            var prior = service is null ? null : existing.FirstOrDefault(x => x.ServiceCatalogItemId == service.Id);
            if (service is not null)
            {
                var contracted = CommercialCalculator.Round(prior?.UnitPrice ?? service.StandardPrice);
                var catalog = CommercialCalculator.Round(service.StandardPrice);
                if (requested != contracted)
                {
                    if (!confirmPriceChanges)
                        return ($"O preço contratado de \"{service.Name}\" é {contracted.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}. Confirme a alteração de preço para usar {requested.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}.", [], notices);
                    price = requested;
                    notices.Add($"Preço de {service.Name} alterado mediante confirmação.");
                }
                else
                {
                    price = contracted;
                    if (prior is not null && catalog != contracted)
                        notices.Add($"O catálogo de {service.Name} está em {catalog.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}. Este orçamento mantém o preço contratado de {contracted.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}.");
                }
            }
            built.Add(new DocumentItem
            {
                DocumentId = documentId,
                ServiceCatalogItemId = service?.Id,
                Description = line.Description.Trim(),
                Unit = string.IsNullOrWhiteSpace(line.Unit) ? "serviço" : line.Unit.Trim(),
                Quantity = line.Quantity,
                UnitPrice = price,
                EstimatedCostSnapshot = prior?.EstimatedCostSnapshot ?? service?.EstimatedCost ?? 0,
                DurationMinutesSnapshot = prior?.DurationMinutesSnapshot ?? service?.SuggestedDurationMinutes,
                Discount = CommercialCalculator.Round(line.Discount),
                Notes = TrimOrNull(line.Notes),
                SortOrder = index
            });
        }
        return (null, built, notices);
    }
    private static string Fingerprint(SaveBudgetDraftRequest request, IReadOnlyList<BudgetWizardItem> lines)
    {
        var builder = new StringBuilder();
        builder.Append(request.ClientId?.ToString("N")).Append('|').Append(request.CurrentStep).Append('|').Append(Day(request.ValidUntil)).Append('|').Append(Day(request.ExpectedStartAt)).Append('|');
        builder.Append(Norm(request.EstimatedDuration)).Append('|').Append(Norm(request.PaymentMethod)).Append('|').Append(request.InstallmentCount).Append('|').Append(Money(request.DepositAmount)).Append('|');
        builder.Append(Norm(request.PixInformation)).Append('|').Append(Norm(request.WarrantyText)).Append('|').Append(Norm(request.ConditionsText)).Append('|');
        builder.Append(Norm(request.TemplateCode).ToLowerInvariant()).Append('|').Append(Money(request.Discount)).Append('|');
        foreach (var line in lines)
        {
            builder.Append(line.ServiceCatalogItemId?.ToString("N")).Append('~').Append(Norm(line.Description)).Append('~').Append(string.IsNullOrWhiteSpace(line.Unit) ? "serviço" : line.Unit.Trim()).Append('~');
            builder.Append(Money(line.Quantity)).Append('~').Append(Money(line.UnitPrice)).Append('~').Append(Money(line.Discount)).Append('~').Append(Norm(line.Notes)).Append(';');
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
    private string StoredFingerprint(Document document)
    {
        var lines = _items.Query().Where(x => x.DocumentId == document.Id && !x.IsDeleted).OrderBy(x => x.SortOrder)
            .Select(x => new BudgetWizardItem(x.ServiceCatalogItemId, x.Description, x.Unit, x.Quantity, x.UnitPrice, x.Discount, x.Notes, x.SortOrder)).ToList();
        var request = new SaveBudgetDraftRequest(document.Id, document.ClientId, document.CurrentWizardStep, document.ValidUntil, document.ExpectedStartAt,
            document.EstimatedDuration, document.PaymentMethod, document.InstallmentCount, document.DepositAmount, document.PixInformation, document.WarrantyText,
            document.ConditionsText, document.TemplateCode, document.Discount, lines, Convert.ToBase64String(document.RowVersion), document.LastAutosaveKey ?? "stored");
        return Fingerprint(request, lines);
    }
    private static string Day(DateTime? value) => value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";
    private static string Norm(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    private static string Money(decimal? value) => CommercialCalculator.Round(value ?? 0).ToString("0.00", CultureInfo.InvariantCulture);
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task SafeRollbackAsync()
    {
        try { await _unitOfWork.RollbackTransactionAsync(); }
        catch (InvalidOperationException) { }
    }
    private static bool IsPersistenceConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var name = current.GetType().Name;
            if (name == "DbUpdateConcurrencyException") return true;
            var state = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;
            if (state is "23505" or "40001") return true;
        }
        return false;
    }
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
        d.TemplateCode, d.Discount, _items.Query().Where(x => x.DocumentId == d.Id && !x.IsDeleted).OrderBy(x => x.SortOrder).Select(x =>
            new BudgetWizardItem(x.ServiceCatalogItemId, x.Description, x.Unit, x.Quantity, x.UnitPrice, x.Discount, x.Notes, x.SortOrder)).ToList(), Convert.ToBase64String(d.RowVersion), d.LastAutosavedAt, d.Number);
}

public sealed class BudgetDraftService(BudgetWizardService wizard)
{
    public Task<BudgetDraftResult> SaveAsync(Guid userId, Guid? accountId, SaveBudgetDraftRequest request, CancellationToken ct) => wizard.SaveAsync(userId, accountId, request, ct);
}
