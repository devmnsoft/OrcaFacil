using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Areas.Admin.Pages.Settings;

[Authorize(Policy = "SuperAdmin")]
public class DatabaseModel : PageModel
{
    private readonly IDatabaseDiagnosticsService _diagnostics;
    public DatabaseDiagnosticsDto? Result { get; private set; }
    public DatabaseModel(IDatabaseDiagnosticsService diagnostics) => _diagnostics = diagnostics;
    public async Task OnGetAsync(CancellationToken ct) => Result = await _diagnostics.CheckAsync(ct);
}
