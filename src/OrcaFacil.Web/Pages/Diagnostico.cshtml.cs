using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Pages;

[Authorize(Policy = "SuperAdmin")]
public class DiagnosticoModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private readonly IDatabaseDiagnosticsService _diagnostics;
    public IDatabaseConfigurationState ConfigurationState { get; }
    public DatabaseDiagnosticsDto? Database { get; private set; }
    public string EnvironmentName => _environment.EnvironmentName;
    public DiagnosticoModel(IWebHostEnvironment environment, IDatabaseDiagnosticsService diagnostics,
        IDatabaseConfigurationState configurationState)
    {
        _environment = environment;
        _diagnostics = diagnostics;
        ConfigurationState = configurationState;
    }
    public async Task OnGetAsync(CancellationToken ct) => Database = await _diagnostics.CheckAsync(ct);
}
