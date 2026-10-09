using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
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
    private readonly ICurrentAccountService _currentAccount;
    private readonly IPlanAccessService _planAccess;
    private readonly IPdfService _pdfService;
    private readonly ICommercialJourneyService _journey;
    private readonly IRepository<Document> _documentRepository;
    private readonly IRepository<IssuerProfile> _profiles;
    private readonly IRepository<UserAccount> _users;
    private readonly IAuditService _audit;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocumentService documents, IDocumentQueries queries, ICurrentUserService currentUser,
        ICurrentAccountService currentAccount, IPlanAccessService planAccess,
        IPdfService pdfService, ICommercialJourneyService journey, IRepository<Document> documentRepository,
        IRepository<IssuerProfile> profiles, IRepository<UserAccount> users, IAuditService audit,
        ILogger<DocumentsController> logger)
    {
        _documents = documents;
        _queries = queries;
        _currentUser = currentUser;
        _currentAccount = currentAccount;
        _planAccess = planAccess;
        _pdfService = pdfService;
        _journey = journey;
        _documentRepository = documentRepository;
        _profiles = profiles;
        _users = users;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public Task<IReadOnlyList<OrcaFacil.Application.DTOs.DocumentSummaryDto>> List(CancellationToken ct) =>
        _queries.ListDocumentsAsync(_currentUser.UserId, ct);

    [HttpPost("budget")]
    public async Task<ActionResult<Result<Guid>>> Budget(CreateDocumentCommand command, CancellationToken ct)
    {
        try
        {
            var accountId = _currentAccount.AccountId;
            var result = await _documents.CreateBudgetAsync(command with
            {
                UserId = _currentUser.UserId,
                AccountId = accountId,
                Type = DocumentType.Budget,
                Number = string.Empty
            }, ct);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar orçamento");
            throw;
        }
    }

    [HttpPost("receipt")]
    public ActionResult Receipt(CreateDocumentCommand command) => BadRequest(new
    {
        Succeeded = false,
        Code = "ReceiptRequiresPayment",
        Message = "Recibos devem ser emitidos a partir de um pagamento registrado. Use a jornada de recebimentos/recibos."
    });

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Result>> Update(Guid id, UpdateDocumentCommand command, CancellationToken ct)
    {
        var result = await _documents.UpdateAsync(command with
        {
            UserId = _currentUser.UserId,
            DocumentId = id,
            AccountId = _currentAccount.AccountId
        }, ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<Result<Guid>>> Duplicate(Guid id, CancellationToken ct)
    {
        var result = await _documents.DuplicateAsync(new DuplicateDocumentCommand(_currentUser.UserId, id, _currentAccount.AccountId), ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        var result = await _documents.DeleteAsync(new DeleteDocumentCommand(_currentUser.UserId, id, _currentAccount.AccountId), ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var document = await _documentRepository.GetAsync(id, ct);
        if (document is null || document.IsDeleted) return NotFound();

        var accountId = _currentAccount.AccountId;
        if (accountId.HasValue && document.AccountId.HasValue && document.AccountId != accountId)
            return Forbid();
        if (!accountId.HasValue && document.UserId != _currentUser.UserId)
            return NotFound();

        var issuer = _profiles.Query().SingleOrDefault(profile => profile.UserId == _currentUser.UserId);

        // Resolve plano comercial efetivo da conta em vez de User.Plan isolado
        var targetAccountId = document.AccountId ?? accountId;
        var effectivePlan = targetAccountId.HasValue
            ? await _planAccess.GetEffectivePlanAsync(targetAccountId.Value, DateTime.UtcNow, ct)
            : null;
        var planType = Enum.TryParse<PlanType>(effectivePlan?.Code, true, out var parsedPlan) ? parsedPlan : PlanType.Free;

        var bytes = await _pdfService.GenerateDocumentPdfAsync(document, issuer, planType, ct);
        await _audit.RegisterAsync(_currentUser.UserId, "PDF_GENERATED", nameof(Document), document.Id.ToString(), null, new { document.Number }, null, ct, document.AccountId);
        return File(bytes, "application/pdf", $"{document.Number}.pdf");
    }

    [HttpPost("{id:guid}/public-link")]
    public async Task<IActionResult> Link(Guid id, CancellationToken ct)
    {
        var result = await _journey.CreatePublicAccessAsync(id, TimeSpan.FromDays(30), ct);
        var body = new
        {
            result.Succeeded,
            Code = result.Code.ToString(),
            result.Message,
            Token = result.PublicToken,
            result.DocumentId,
            result.RevisionId,
            result.PublicAccessId,
            Status = result.CurrentStatus?.ToString(),
            result.CorrelationId
        };
        return result.Succeeded ? Ok(body) : BadRequest(body);
    }
}
