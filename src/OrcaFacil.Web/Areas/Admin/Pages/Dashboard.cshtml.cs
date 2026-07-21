using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Diagnostics;

namespace OrcaFacil.Web.Areas.Admin.Pages;

[Authorize(Policy = "SuperAdmin")]
public class DashboardModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    private readonly DatabaseDiagnosticsService _diagnostics;
    public int TotalUsers { get; private set; }
    public int TotalDocuments { get; private set; }
    public int FreeUsers { get; private set; }
    public int ProUsers { get; private set; }
    public DatabaseDiagnosticsResult? Database { get; private set; }
    public DashboardModel(OrcaFacilDbContext db, DatabaseDiagnosticsService diagnostics) { _db = db; _diagnostics = diagnostics; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        TotalUsers = await _db.Users.CountAsync(ct);
        TotalDocuments = await _db.Documents.CountAsync(ct);
        FreeUsers = await _db.Users.CountAsync(u => u.Plan == PlanType.Free, ct);
        ProUsers = await _db.Users.CountAsync(u => u.Plan == PlanType.Pro, ct);
        Database = await _diagnostics.CheckAsync(ct);
    }
}
