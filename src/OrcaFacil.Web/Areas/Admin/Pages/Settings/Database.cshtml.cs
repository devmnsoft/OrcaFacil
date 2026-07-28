using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Areas.Admin.Pages.Settings;

[Authorize(Policy = "SuperAdminOnly")]
public class DatabaseModel : PageModel
{
    private readonly IDatabaseDiagnosticsService _diagnostics;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    public DatabaseDiagnosticsDto? Result { get; private set; }
    public DatabaseConnectionDescriptor? Descriptor { get; private set; }
    public string? LocalSettingsPath { get; private set; }
    public bool LocalSettingsMissing { get; private set; }
    public DatabaseModel(IDatabaseDiagnosticsService diagnostics, IConfiguration configuration, IHostEnvironment environment) =>
        (_diagnostics, _configuration, _environment) = (diagnostics, configuration, environment);
    public async Task OnGetAsync(CancellationToken ct)
    {
        if (_environment.IsDevelopment())
        {
            LocalSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.Local.json");
            LocalSettingsMissing = !System.IO.File.Exists(LocalSettingsPath);
        }
        Result = await _diagnostics.CheckAsync(ct);
        if (DatabaseConnectionOptions.TryCreate(_configuration, out var options, out _))
            Descriptor = DatabaseConnectionDescriptor.From(options!, _environment.EnvironmentName);
    }
}
