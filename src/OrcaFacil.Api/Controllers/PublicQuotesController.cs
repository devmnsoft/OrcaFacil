using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public-quotes")]
public class PublicQuotesController : ControllerBase
{
    private readonly IPublicDocumentAccessService _access;
    private readonly ICommercialJourneyService _journey;
    private readonly IPdfService _pdfService;
    private readonly ILogger<PublicQuotesController> _logger;

    public PublicQuotesController(
        IPublicDocumentAccessService access,
        ICommercialJourneyService journey,
        IPdfService pdfService,
        ILogger<PublicQuotesController> logger)
    {
        _access = access;
        _journey = journey;
        _pdfService = pdfService;
        _logger = logger;
    }

    [HttpPost("{token}/approve")]
    public async Task<IActionResult> Approve(string token, ApprovePublicQuoteCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _journey.DecideAsync(token, PublicDocumentDecisionType.Approved,
                command.Name, command.Email ?? command.Document, null, command.Note, null,
                command.AcceptedTerms, IdempotencyKey(),
                RemoteAddress(), Request.Headers.UserAgent.ToString(), ct);
            return ToDecisionResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar orçamento público");
            throw;
        }
    }

    [HttpPost("{token}/reject")]
    public async Task<IActionResult> Reject(string token, RejectPublicQuoteCommand command, CancellationToken ct)
    {
        var result = await _journey.DecideAsync(token, PublicDocumentDecisionType.Rejected,
            command.Name, command.Contact, command.Reason, command.Message, null, false,
            command.IdempotencyKey ?? IdempotencyKey(),
            RemoteAddress(), Request.Headers.UserAgent.ToString(), ct);
        return ToDecisionResponse(result);
    }

    [HttpGet("{token}/pdf")]
    public async Task<IActionResult> Pdf(string token, CancellationToken ct)
    {
        var opened = await _access.OpenAsync(token, RemoteAddress(), Request.Headers.UserAgent.ToString(), ct);
        if (!opened.Succeeded || opened.Value is null)
            return ToPublicAccessResponse(opened);

        var quote = opened.Value;
        var document = FromSnapshot(quote);
        var issuer = FromSnapshot(quote.Snapshot.Issuer);
        var plan = quote.Snapshot.Quote.ShowPlatformBrand ? PlanType.Free : PlanType.Professional;
        var lang = string.IsNullOrWhiteSpace(quote.Snapshot.Quote.LanguageCode) ? "pt-BR" : quote.Snapshot.Quote.LanguageCode;
        var curr = string.IsNullOrWhiteSpace(quote.Snapshot.Quote.CurrencyCode) ? "BRL" : quote.Snapshot.Quote.CurrencyCode;
        var bytes = await _pdfService.GenerateDocumentPdfAsync(document, issuer, plan, lang, curr, ct);
        return File(bytes, "application/pdf", $"{SafeFileName(document.Number)}.pdf");
    }

    private IActionResult ToDecisionResponse(PublicDecisionResult result)
    {
        var body = new
        {
            result.Succeeded,
            Code = result.Code.ToString(),
            result.Message,
            result.DocumentId,
            result.RevisionId,
            result.PublicAccessId,
            Status = result.CurrentStatus?.ToString(),
            result.CorrelationId,
            result.DecisionId
        };
        if (result.Succeeded) return Ok(body);
        return result.Code switch
        {
            QuoteLifecycleCode.PublicLinkUnavailable => NotFound(body),
            QuoteLifecycleCode.PublicLinkExpired or QuoteLifecycleCode.PublicLinkRevoked or QuoteLifecycleCode.VersionOutdated => StatusCode(StatusCodes.Status410Gone, body),
            QuoteLifecycleCode.DecisionAlreadyRegistered or QuoteLifecycleCode.IdempotencyConflict or QuoteLifecycleCode.ConcurrencyConflict => Conflict(body),
            _ => BadRequest(body)
        };
    }

    private IActionResult ToPublicAccessResponse(OperationResult<PublicQuoteView> result)
    {
        var body = new { result.Succeeded, result.Code, result.Message };
        return result.Code switch
        {
            "PublicLinkUnavailable" => NotFound(body),
            "PublicLinkExpired" or "PublicLinkRevoked" or "VersionOutdated" => StatusCode(StatusCodes.Status410Gone, body),
            _ => BadRequest(body)
        };
    }

    private string RemoteAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private string IdempotencyKey() =>
        Request.Headers.TryGetValue("Idempotency-Key", out var values) && !string.IsNullOrWhiteSpace(values.FirstOrDefault())
            ? values.First()!
            : Guid.NewGuid().ToString("N");

    private static Document FromSnapshot(PublicQuoteView quote)
    {
        var snapshot = quote.Snapshot;
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
            EstimatedDuration = snapshot.Quote.DeliveryTime,
            PaymentMethod = snapshot.Quote.Payment,
            ConditionsText = snapshot.Quote.Conditions,
            WarrantyText = snapshot.Quote.Footer,
            Notes = snapshot.Quote.Notes,
            Discount = snapshot.Quote.Discount,
            TemplateCode = string.IsNullOrWhiteSpace(snapshot.Quote.Template) ? "essential" : snapshot.Quote.Template
        };
        document.IssueNumber(snapshot.Quote.Number);
        document.Items = snapshot.Items.Select((item, index) => new DocumentItem
        {
            Description = item.Description,
            Unit = string.IsNullOrWhiteSpace(item.Unit) ? "un" : item.Unit,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Discount = item.Discount,
            SortOrder = index
        }).ToList();
        document.CalculateTotals();
        return document;
    }

    private static IssuerProfile FromSnapshot(IssuerSnapshot issuer) => new()
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

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((string.IsNullOrWhiteSpace(value) ? "orcamento" : value)
            .Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "orcamento" : cleaned;
    }

    public sealed record RejectPublicQuoteCommand(
        string Name,
        string Contact,
        string Reason,
        string? Message,
        string? IdempotencyKey);
}
