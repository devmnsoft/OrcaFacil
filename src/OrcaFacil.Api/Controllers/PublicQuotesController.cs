using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcaFacil.Application.Documents;
using OrcaFacil.Shared;

namespace OrcaFacil.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public-quotes")]
public class PublicQuotesController : ControllerBase
{
    private readonly DocumentService _documents;
    private readonly ILogger<PublicQuotesController> _logger;

    public PublicQuotesController(DocumentService documents, ILogger<PublicQuotesController> logger)
    {
        _documents = documents;
        _logger = logger;
    }

    [HttpPost("{token}/approve")]
    public async Task<ActionResult<Result>> Approve(string token, ApprovePublicQuoteCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _documents.ApproveAsync(command with { Token = token, UserAgent = Request.Headers.UserAgent.ToString() }, ct);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar orçamento público");
            throw;
        }
    }

    [HttpPost("{token}/reject")]
    public IActionResult Reject(string token) => Accepted();

    [HttpGet("{token}/pdf")]
    public IActionResult Pdf(string token) => File(Array.Empty<byte>(), "application/pdf");
}
