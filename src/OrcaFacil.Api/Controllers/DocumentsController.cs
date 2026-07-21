using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
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
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocumentService documents, IDocumentQueries queries, ICurrentUserService currentUser, ILogger<DocumentsController> logger)
    {
        _documents = documents;
        _queries = queries;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public Task<IReadOnlyList<OrcaFacil.Application.DTOs.DocumentSummaryDto>> List(CancellationToken ct) => _queries.ListDocumentsAsync(_currentUser.UserId, ct);

    [HttpPost("budget")]
    public async Task<ActionResult<Result<Guid>>> Budget(CreateDocumentCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _documents.CreateAsync(command with { UserId = _currentUser.UserId, Type = DocumentType.Budget }, ct);
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
            var result = await _documents.CreateAsync(command with { UserId = _currentUser.UserId, Type = DocumentType.Receipt }, ct);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar recibo");
            throw;
        }
    }

    [HttpPost("{id:guid}/public-link")]
    public Task<Result<string>> Link(Guid id, CancellationToken ct) => _documents.GeneratePublicLinkAsync(_currentUser.UserId, id, ct);
}
