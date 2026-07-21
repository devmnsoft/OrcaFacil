using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Diagnostics;

namespace OrcaFacil.Web.Areas.Admin.Pages.Settings;

[Authorize(Policy = "SuperAdmin")]
public class DatabaseModel : PageModel
{
    private readonly DatabaseDiagnosticsService _diagnostics;
    public DatabaseDiagnosticsResult? Result { get; private set; }
    public DatabaseModel(DatabaseDiagnosticsService diagnostics) => _diagnostics = diagnostics;
    public async Task OnGetAsync(CancellationToken ct) => Result = await _diagnostics.CheckAsync(ct);
}
