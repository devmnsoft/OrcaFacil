using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Web.Pages.PublicQuotes;

[AllowAnonymous]
public sealed class ViewModel(IPublicDocumentAccessService access, ICommercialJourneyService journey) : PageModel
{
    public PublicQuoteView? Quote { get; private set; }
    public string? LoadError { get; private set; }

    [BindProperty]
    public DecisionInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string token, CancellationToken ct)
    {
        var result = await access.OpenAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), ct);
        if (!result.Succeeded) { LoadError = result.Message; return Page(); }
        Quote = result.Value;
        return Page();
    }

    public Task<IActionResult> OnPostApproveAsync(string token, CancellationToken ct) => Decide(token, PublicDocumentDecisionType.Approved, ct);
    public Task<IActionResult> OnPostChangeAsync(string token, CancellationToken ct) => Decide(token, PublicDocumentDecisionType.ChangeRequested, ct);
    public Task<IActionResult> OnPostRejectAsync(string token, CancellationToken ct) => Decide(token, PublicDocumentDecisionType.Rejected, ct);

    private async Task<IActionResult> Decide(string token, PublicDocumentDecisionType decision, CancellationToken ct)
    {
        if (decision == PublicDocumentDecisionType.ChangeRequested && string.IsNullOrWhiteSpace(Input.Message))
            ModelState.AddModelError(nameof(Input.Message), "Conte o que precisa ser alterado.");
        if (decision == PublicDocumentDecisionType.Rejected && string.IsNullOrWhiteSpace(Input.Reason))
            ModelState.AddModelError(nameof(Input.Reason), "Selecione o principal motivo da recusa.");
        if (decision == PublicDocumentDecisionType.Approved && !Input.AcceptedTerms)
            ModelState.AddModelError(nameof(Input.AcceptedTerms), "Confirme que leu e aceita as condições da proposta.");
        if (!ModelState.IsValid) { await OnGetAsync(token, ct); return Page(); }
        var result = await journey.DecideAsync(token, decision, Input.Name, Input.Contact, Input.Reason, Input.Message,
            Input.DesiredDate, Input.AcceptedTerms,
            string.IsNullOrWhiteSpace(Input.IdempotencyKey) ? Guid.NewGuid().ToString("N") : Input.IdempotencyKey,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", Request.Headers.UserAgent.ToString(), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { token });
    }

    public sealed class DecisionInput
    {
        [Required(ErrorMessage = "Informe o nome do responsável."), StringLength(180)] public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Informe um e-mail ou telefone para retorno."), StringLength(254)] public string Contact { get; set; } = string.Empty;
        [StringLength(40)] public string? Reason { get; set; }
        [StringLength(1000)] public string? Message { get; set; }
        [DataType(DataType.Date)] public DateTime? DesiredDate { get; set; }
        public bool AcceptedTerms { get; set; }
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    }
}
