using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.ValueObjects;
using OrcaFacil.Shared;

namespace OrcaFacil.Application.Documents;

public class DocumentService
{
    private readonly IRepository<Document> _documents;
    private readonly IRepository<PublicQuote> _quotes;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly IDocumentNumberService _numberService;
    private readonly ILogger<DocumentService> _logger;
    private readonly INotificationService _notifications;

    public DocumentService(IRepository<Document> documents, IRepository<PublicQuote> quotes, IUnitOfWork uow, IAuditService audit, IDocumentNumberService numberService, ILogger<DocumentService> logger, INotificationService notifications)
    {
        _documents = documents;
        _quotes = quotes;
        _uow = uow;
        _audit = audit;
        _numberService = numberService;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task<Result<Guid>> CreateAsync(CreateDocumentCommand command, CancellationToken ct = default)
    {
        try
        {
            var document = new Document { UserId = command.UserId, Type = command.Type, ClientName = command.ClientName.Trim(), Discount = command.Discount, Notes = command.Notes };
            document.IssueNumber(string.IsNullOrWhiteSpace(command.Number) ? await _numberService.NextAsync(command.UserId, command.Type, ct) : command.Number);
            document.Items = command.Items.Select(item => new DocumentItem { Description = item.Description, Quantity = item.Quantity, UnitPrice = item.UnitPrice, Discount = item.Discount }).ToList();
            document.CalculateTotals();
            await _documents.AddAsync(document, ct);
            await _audit.RegisterAsync(command.UserId, "DOCUMENT_CREATED", nameof(Document), document.Id.ToString(), null, document, null, ct);
            await _uow.SaveChangesAsync(ct);
            var title = document.Type == DocumentType.Budget ? "Orçamento salvo" : "Recibo salvo";
            await _notifications.CreateForUserAsync(command.UserId, title, "Seu orçamento foi salvo. Agora você pode gerar o PDF.", NotificationType.Success, NotificationCategory.Document, $"/Documents/Details?id={document.Id}", "Ver documento", ct);
            if (_documents.Query().Count(x => x.UserId == command.UserId && !x.IsDeleted) == 1)
            {
                await _notifications.CreateForUserAsync(command.UserId, "Primeiro orçamento criado", "Parabéns pelo primeiro documento no OrçaFácil. Gere o PDF e envie ao cliente.", NotificationType.Info, NotificationCategory.Document, $"/Documents/Details?id={document.Id}", "Abrir", ct);
            }
            _logger.LogInformation("DOCUMENT_CREATED {DocumentId}", document.Id);
            return Result<Guid>.Ok(document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar documento para {UserId}", command.UserId);
            throw;
        }
    }


    public Task<Result<Guid>> CreateBudgetAsync(CreateDocumentCommand command, CancellationToken ct = default)
        => CreateAsync(command with { Type = DocumentType.Budget }, ct);

    [Obsolete("Use IReceiptApplicationService; legacy Documents remain read-only.")]
    public Task<Result<Guid>> CreateReceiptAsync(CreateDocumentCommand command, CancellationToken ct = default)
        => CreateAsync(command with { Type = DocumentType.Receipt }, ct);

    public async Task<Result> UpdateAsync(UpdateDocumentCommand command, CancellationToken ct = default)
    {
        try
        {
            var document = await _documents.GetAsync(command.DocumentId, ct);
            if (document is null || document.UserId != command.UserId || document.IsDeleted) return Result.Fail("Documento não encontrado.");
            document.ClientName = command.ClientName.Trim();
            document.Discount = command.Discount;
            document.Notes = command.Notes;
            document.Items = command.Items.Select(item => new DocumentItem { Description = item.Description, Quantity = item.Quantity, UnitPrice = item.UnitPrice, Discount = item.Discount }).ToList();
            document.CalculateTotals();
            document.Touch();
            await _audit.RegisterAsync(command.UserId, "DOCUMENT_UPDATED", nameof(Document), document.Id.ToString(), null, document, null, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("DOCUMENT_UPDATED {DocumentId}", document.Id);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar documento {DocumentId}", command.DocumentId);
            throw;
        }
    }

    public async Task<Result<Guid>> DuplicateAsync(DuplicateDocumentCommand command, CancellationToken ct = default)
    {
        var original = await _documents.GetAsync(command.DocumentId, ct);
        if (original is null || original.UserId != command.UserId || original.IsDeleted) return Result<Guid>.Fail("Documento não encontrado.");
        var copy = new Document { UserId = original.UserId, Type = original.Type, Status = "Draft", ClientName = original.ClientName, ClientDocument = original.ClientDocument, ClientPhone = original.ClientPhone, ClientEmail = original.ClientEmail, ClientCity = original.ClientCity, IssueDate = DateTime.UtcNow, Notes = original.Notes, Discount = original.Discount };
        copy.IssueNumber(await _numberService.NextAsync(command.UserId, original.Type, ct));
        copy.Items = original.Items.Select(item => new DocumentItem { Description = item.Description, Quantity = item.Quantity, UnitPrice = item.UnitPrice, Discount = item.Discount }).ToList();
        copy.CalculateTotals();
        await _documents.AddAsync(copy, ct);
        await _audit.RegisterAsync(command.UserId, "DOCUMENT_DUPLICATED", nameof(Document), copy.Id.ToString(), null, copy, new { original.Id }, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Guid>.Ok(copy.Id);
    }

    public async Task<Result> DeleteAsync(DeleteDocumentCommand command, CancellationToken ct = default)
    {
        var document = await _documents.GetAsync(command.DocumentId, ct);
        if (document is null || document.UserId != command.UserId || document.IsDeleted) return Result.Fail("Documento não encontrado.");
        document.Delete(command.UserId);
        await _audit.RegisterAsync(command.UserId, "DOCUMENT_DELETED", nameof(Document), document.Id.ToString(), null, new { document.DeletedAt }, null, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    [Obsolete("Use ICommercialJourneyService.CreatePublicAccessAsync.")]
    public async Task<Result<string>> GeneratePublicLinkAsync(Guid userId, Guid documentId, CancellationToken ct = default)
    {
        try
        {
            var document = await _documents.GetAsync(documentId, ct);
            if (document is null || document.UserId != userId) return Result<string>.Fail("Documento não encontrado.");
            if (document.RequiresInternalApproval && document.InternalApprovalStatus != ApprovalStatus.Approved)
                return Result<string>.Fail("Este orçamento precisa de aprovação interna antes de ser enviado ao cliente.");
            document.PublicToken = new PublicToken().Value;
            document.PublicEnabled = true;
            var quote = new PublicQuote { DocumentId = document.Id, OwnerUserId = userId, Token = document.PublicToken, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            await _quotes.AddAsync(quote, ct);
            await _audit.RegisterAsync(userId, "PUBLIC_LINK_CREATED", nameof(Document), document.Id.ToString(), null, new { quote.Token }, null, ct);
            await _uow.SaveChangesAsync(ct);
            return Result<string>.Ok(quote.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar link público para {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<Result> ApproveAsync(ApprovePublicQuoteCommand command, CancellationToken ct = default)
    {
        try
        {
            var quote = _quotes.Query().SingleOrDefault(item => item.Token == command.Token && item.PublicEnabled);
            if (quote is null) return Result.Fail("Link inválido.");
            if (!command.AcceptedTerms) return Result.Fail("Aceite dos termos é obrigatório.");
            var document = await _documents.GetAsync(quote.DocumentId, ct);
            if (document is null) return Result.Fail("Documento indisponível.");
            quote.DecisionStatus = ClientDecision.Approved;
            quote.DecisionNote = command.Note;
            quote.DecidedAt = DateTime.UtcNow;
            quote.DecidedByName = command.Name;
            quote.DecidedByDocument = command.Document;
            quote.DecidedByEmail = command.Email;
            quote.AcceptedTerms = command.AcceptedTerms;
            quote.UserAgent = command.UserAgent;
            document.ClientDecision = ClientDecision.Approved;
            document.ClientDecisionAt = quote.DecidedAt;
            document.Status = BudgetStatus.Approved.ToString();
            quote.EvidenceHash = document.GenerateEvidenceHash(command.Name, command.UserAgent);
            await _audit.RegisterAsync(null, "PUBLIC_QUOTE_APPROVED", nameof(PublicQuote), quote.Id.ToString(), null, quote, new { document.Id }, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar orçamento público");
            throw;
        }
    }
}
