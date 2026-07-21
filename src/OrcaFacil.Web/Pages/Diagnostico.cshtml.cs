using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Diagnostics;

namespace OrcaFacil.Web.Pages;

public class DiagnosticoModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly DatabaseDiagnosticsService _diagnostics;
    public DatabaseDiagnosticsResult? Database { get; private set; }
    public string EnvironmentName => _environment.EnvironmentName;
    public DiagnosticoModel(IWebHostEnvironment environment, DatabaseDiagnosticsService diagnostics)
    {
        _environment = environment;
        _diagnostics = diagnostics;
    }
    public async Task OnGetAsync(CancellationToken ct) => Database = await _diagnostics.CheckAsync(ct);
}
