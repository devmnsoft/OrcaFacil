using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Shared;

namespace OrcaFacil.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documents;
    private readonly IDocumentQueries _queries;
    private readonly ICurrentUserService _currentUser;
    private readonly IPdfService _pdfService;
    private readonly IRepository<Document> _documentRepository;
    private readonly IRepository<IssuerProfile> _profiles;
    private readonly IRepository<UserAccount> _users;
    private readonly IAuditService _audit;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocumentService documents, IDocumentQueries queries, ICurrentUserService currentUser, IPdfService pdfService, IRepository<Document> documentRepository, IRepository<IssuerProfile> profiles, IRepository<UserAccount> users, IAuditService audit, ILogger<DocumentsController> logger)
    {
        _documents = documents;
        _queries = queries;
        _currentUser = currentUser;
        _pdfService = pdfService;
        _documentRepository = documentRepository;
        _profiles = profiles;
        _users = users;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public Task<IReadOnlyList<OrcaFacil.Application.DTOs.DocumentSummaryDto>> List(CancellationToken ct) => _queries.ListDocumentsAsync(_currentUser.UserId, ct);

    [HttpPost("budget")]
    public async Task<ActionResult<Result<Guid>>> Budget(CreateDocumentCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _documents.CreateBudgetAsync(command with { UserId = _currentUser.UserId, Type = DocumentType.Budget, Number = string.Empty }, ct);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar orçamento");
            throw;
        }
    }

    [HttpPost("receipt")]
    public async Task<ActionResult<Result<Guid>>> Receipt(CreateDocumentCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _documents.CreateReceiptAsync(command with { UserId = _currentUser.UserId, Type = DocumentType.Receipt, Number = string.Empty }, ct);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar recibo");
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result>> Update(Guid id, UpdateDocumentCommand command, CancellationToken ct)
    {
        var result = await _documents.UpdateAsync(command with { UserId = _currentUser.UserId, DocumentId = id }, ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<Result<Guid>>> Duplicate(Guid id, CancellationToken ct)
    {
        var result = await _documents.DuplicateAsync(new DuplicateDocumentCommand(_currentUser.UserId, id), ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _documents.DeleteAsync(new DeleteDocumentCommand(_currentUser.UserId, id), ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var document = await _documentRepository.GetAsync(id, ct);
        if (document is null || document.UserId != _currentUser.UserId || document.IsDeleted) return NotFound();
        var issuer = _profiles.Query().SingleOrDefault(profile => profile.UserId == _currentUser.UserId);
        var user = await _users.GetAsync(_currentUser.UserId, ct);
        var plan = user?.Plan ?? PlanType.Free;
        var bytes = await _pdfService.GenerateDocumentPdfAsync(document, issuer, plan, ct);
        await _audit.RegisterAsync(_currentUser.UserId, "PDF_GENERATED", nameof(Document), document.Id.ToString(), null, new { document.Number }, null, ct);
        return File(bytes, "application/pdf", $"{document.Number}.pdf");
    }

    [HttpPost("{id:guid}/public-link")]
    public Task<Result<string>> Link(Guid id, CancellationToken ct) => _documents.GeneratePublicLinkAsync(_currentUser.UserId, id, ct);
}
