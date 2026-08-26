using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrcaFacil.Web.Pages;

[AllowAnonymous]
public sealed class DomainUnavailableModel : PageModel
{
    public string CorrelationId { get; private set; } = string.Empty;
    public void OnGet() => CorrelationId = HttpContext.TraceIdentifier;
}
