using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Areas.Admin.Pages.Settings;

[Authorize(Policy = "SuperAdminOnly")]
public class DatabaseModel : PageModel
{
    private readonly IDatabaseDiagnosticsService _diagnostics;
    private readonly IHostEnvironment _environment;
    public IDatabaseConfigurationState ConfigurationState { get; }
    public DatabaseDiagnosticsDto? Result { get; private set; }
    public DatabaseConnectionDescriptor? Descriptor { get; private set; }
    public string? LocalSettingsPath { get; private set; }
    public bool LocalSettingsMissing { get; private set; }
    public DatabaseModel(IDatabaseDiagnosticsService diagnostics, IDatabaseConfigurationState configurationState, IHostEnvironment environment) =>
        (_diagnostics, ConfigurationState, _environment) = (diagnostics, configurationState, environment);
    public async Task OnGetAsync(CancellationToken ct)
    {
        if (_environment.IsDevelopment())
        {
            LocalSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.Local.json");
            LocalSettingsMissing = !System.IO.File.Exists(LocalSettingsPath);
        }
        if (ConfigurationState.IsValid)
            Result = await _diagnostics.CheckAsync(ct);
    }
}
