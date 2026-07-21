using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrcaFacil.Web.Pages;

[AllowAnonymous]
public class ErrorModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    public ErrorModel(IWebHostEnvironment environment) => _environment = environment;
    public string CorrelationId { get; private set; } = string.Empty;
    public bool ShowTechnicalDetails => _environment.IsDevelopment() && !string.IsNullOrWhiteSpace(TechnicalDetails);
    public string? TechnicalDetails { get; private set; }
    public void OnGet()
    {
        CorrelationId = HttpContext.TraceIdentifier;
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (_environment.IsDevelopment() && feature?.Error is not null)
        {
            TechnicalDetails = feature.Error.Message;
        }
    }
}
