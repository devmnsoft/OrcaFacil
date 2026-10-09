using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
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
    private readonly OrcaFacilDbContext _db;
    private readonly IAuditService _audit;
    private readonly ICommercialRevisionResolver _revisionResolver;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocumentService documents, IDocumentQueries queries, ICurrentUserService currentUser,
        ICurrentAccountService currentAccount, IPlanAccessService planAccess,
        IPdfService pdfService, ICommercialJourneyService journey, IRepository<Document> documentRepository,
        IRepository<IssuerProfile> profiles, IRepository<UserAccount> users, OrcaFacilDbContext db, IAuditService audit,
        ICommercialRevisionResolver revisionResolver,
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
        _db = db;
        _audit = audit;
        _revisionResolver = revisionResolver;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrcaFacil.Application.DTOs.DocumentSummaryDto>>> List(CancellationToken ct)
    {
        await _currentAccount.EnsureAccountAccessAsync(ct);
        if (_currentAccount.AccountId.HasValue && !await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsView, ct))
            return Forbid();

        var list = await _queries.ListDocumentsAsync(_currentUser.UserId, _currentAccount.AccountId, ct);
        return Ok(list);
    }

    [HttpPost("budget")]
    public async Task<ActionResult<Result<Guid>>> Budget(CreateDocumentCommand command, CancellationToken ct)
    {
        try
        {
            await _currentAccount.EnsureAccountAccessAsync(ct);
            if (_currentAccount.AccountId.HasValue && !await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct))
                return Forbid();

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
        await _currentAccount.EnsureAccountAccessAsync(ct);
        if (_currentAccount.AccountId.HasValue && !await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct))
            return Forbid();

        var document = await _documentRepository.GetAsync(id, ct);
        if (document is null || document.IsDeleted) return NotFound();

        if (_currentAccount.AccountId.HasValue)
        {
            if (document.AccountId != _currentAccount.AccountId)
                return Forbid();
        }
        else
        {
            if (document.UserId != _currentUser.UserId || document.AccountId != null)
                return NotFound();
        }

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
        await _currentAccount.EnsureAccountAccessAsync(ct);
        if (_currentAccount.AccountId.HasValue && !await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct))
            return Forbid();

        var original = await _documentRepository.GetAsync(id, ct);
        if (original is null || original.IsDeleted) return NotFound();

        if (_currentAccount.AccountId.HasValue)
        {
            if (original.AccountId != _currentAccount.AccountId)
                return Forbid();
        }
        else
        {
            if (original.UserId != _currentUser.UserId || original.AccountId != null)
                return NotFound();
        }

        var result = await _documents.DuplicateAsync(new DuplicateDocumentCommand(_currentUser.UserId, id, _currentAccount.AccountId), ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result>> Delete(Guid id, CancellationToken ct)
    {
        await _currentAccount.EnsureAccountAccessAsync(ct);
        if (_currentAccount.AccountId.HasValue && !await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct))
            return Forbid();

        var document = await _documentRepository.GetAsync(id, ct);
        if (document is null || document.IsDeleted) return NotFound();

        if (_currentAccount.AccountId.HasValue)
        {
            if (document.AccountId != _currentAccount.AccountId)
                return Forbid();
        }
        else
        {
            if (document.UserId != _currentUser.UserId || document.AccountId != null)
                return NotFound();
        }

        var result = await _documents.DeleteAsync(new DeleteDocumentCommand(_currentUser.UserId, id, _currentAccount.AccountId), ct);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        await _currentAccount.EnsureAccountAccessAsync(ct);
        if (_currentAccount.AccountId.HasValue && !await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsView, ct))
            return Forbid();

        var document = await _documentRepository.GetAsync(id, ct);
        if (document is null || document.IsDeleted) return NotFound();

        var accountId = _currentAccount.AccountId;
        if (accountId.HasValue)
        {
            if (document.AccountId != accountId)
                return Forbid();
        }
        else
        {
            if (document.UserId != _currentUser.UserId || document.AccountId != null)
                return NotFound();
        }

        var targetAccountId = document.AccountId ?? accountId;
        var effectivePlan = targetAccountId.HasValue
            ? await _planAccess.GetEffectivePlanAsync(targetAccountId.Value, DateTime.UtcNow, ct)
            : null;
        var planType = Enum.TryParse<PlanType>(effectivePlan?.Code, true, out var parsedPlan) ? parsedPlan : PlanType.Free;

        DocumentRevision? currentRevision = null;
        if (targetAccountId.HasValue)
        {
            currentRevision = await _db.DocumentRevisions.AsNoTracking().FirstOrDefaultAsync(
                x => x.AccountId == targetAccountId.Value && x.DocumentId == document.Id && x.IsCurrent, ct);
        }

        IssuerProfile? defaultIssuer = null;
        if (targetAccountId.HasValue)
        {
            var businessAccount = await _db.BusinessAccounts.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == targetAccountId.Value && !x.IsDeleted, ct);
            var accountSettings = await _db.AccountSettings.AsNoTracking().SingleOrDefaultAsync(
                x => x.AccountId == targetAccountId.Value && !x.IsDeleted, ct);

            if (businessAccount is not null)
            {
                defaultIssuer = new IssuerProfile
                {
                    BusinessName = businessAccount.TradeName ?? businessAccount.DisplayName,
                    DocumentNumber = businessAccount.DocumentNumber,
                    Email = businessAccount.Email,
                    Phone = businessAccount.Phone ?? accountSettings?.WhatsApp,
                    Address = accountSettings?.Address,
                    City = accountSettings?.City,
                    PixKey = accountSettings?.PixKey,
                    LogoPath = accountSettings?.LogoPath ?? accountSettings?.CompactLogoPath
                };
            }
            else
            {
                defaultIssuer = _profiles.Query().SingleOrDefault(profile => profile.UserId == document.UserId);
            }
        }
        else
        {
            defaultIssuer = _profiles.Query().SingleOrDefault(profile => profile.UserId == _currentUser.UserId);
        }

        var resolution = _revisionResolver.Resolve(document, currentRevision, defaultIssuer, planType);
        if (!resolution.Succeeded)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity,
                new { code = resolution.Code, message = resolution.Message, documentId = document.Id });
        }

        var resolved = resolution.Value!;
        var bytes = await _pdfService.GenerateDocumentPdfAsync(
            resolved.Document, resolved.Issuer, resolved.EffectivePlan, resolved.LanguageCode, resolved.CurrencyCode, ct);
        await _audit.RegisterAsync(_currentUser.UserId, "PDF_GENERATED", nameof(Document), document.Id.ToString(), null, new { document.Number, RevisionId = currentRevision?.Id }, null, ct, document.AccountId);
        return File(bytes, "application/pdf", $"{document.Number}.pdf");
    }

    [HttpPost("{id:guid}/public-link")]
    public async Task<IActionResult> Link(Guid id, CancellationToken ct)
    {
        await _currentAccount.EnsureAccountAccessAsync(ct);
        if (_currentAccount.AccountId.HasValue && !await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsGeneratePublicLink, ct))
            return Forbid();

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

    private static Document FromSnapshot(DocumentSnapshot snapshot)
    {
        var document = new Document
        {
            Type = DocumentType.Budget,
            Status = "Sent",
            ClientName = snapshot.Customer.Name,
            ClientDocument = snapshot.Customer.Document,
            ClientPhone = snapshot.Customer.Phone,
            ClientEmail = snapshot.Customer.Email,
            ClientCity = snapshot.Customer.City,
            IssueDate = snapshot.Quote.IssueDate,
            ValidUntil = snapshot.Quote.ValidUntil,
            Notes = snapshot.Quote.Notes,
            Discount = snapshot.Quote.Discount,
            TemplateCode = snapshot.Quote.Template
        };
        document.IssueNumber(snapshot.Quote.Number);
        document.Items = snapshot.Items.Select((item, index) => new DocumentItem
        {
            Description = item.Description,
            Unit = item.Unit ?? "serviço",
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Discount = item.Discount,
            SortOrder = index
        }).ToList();
        document.CalculateTotals();
        return document;
    }

    private static IssuerProfile FromIssuerSnapshot(IssuerSnapshot issuer) => new()
    {
        BusinessName = issuer.Name,
        DocumentNumber = issuer.Document,
        Email = issuer.Email,
        Phone = issuer.Phone,
        Address = issuer.Address,
        City = issuer.City,
        PixKey = issuer.Pix,
        LogoPath = issuer.Logo
    };
}
