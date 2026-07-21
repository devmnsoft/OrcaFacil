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
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IRepository<Document> documents, IRepository<PublicQuote> quotes, IUnitOfWork uow, IAuditService audit, ILogger<DocumentService> logger)
    {
        _documents = documents;
        _quotes = quotes;
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<Guid>> CreateAsync(CreateDocumentCommand command, CancellationToken ct = default)
    {
        try
        {
            var document = new Document { UserId = command.UserId, Type = command.Type, ClientName = command.ClientName.Trim(), Discount = command.Discount, Notes = command.Notes };
            document.IssueNumber(command.Number);
            document.Items = command.Items.Select(item => new DocumentItem { Description = item.Description, Quantity = item.Quantity, UnitPrice = item.UnitPrice, Discount = item.Discount }).ToList();
            document.CalculateTotals();
            await _documents.AddAsync(document, ct);
            await _audit.RegisterAsync(command.UserId, "DOCUMENT_CREATED", nameof(Document), document.Id.ToString(), null, document, null, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("DOCUMENT_CREATED {DocumentId}", document.Id);
            return Result<Guid>.Ok(document.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar documento para {UserId}", command.UserId);
            throw;
        }
    }

    public async Task<Result<string>> GeneratePublicLinkAsync(Guid userId, Guid documentId, CancellationToken ct = default)
    {
        try
        {
            var document = await _documents.GetAsync(documentId, ct);
            if (document is null || document.UserId != userId) return Result<string>.Fail("Documento não encontrado.");
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
