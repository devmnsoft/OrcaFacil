using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Areas.Admin.Pages.SystemLogs;
[Authorize(Policy="PlatformAuditRead")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
 public IReadOnlyList<Row> Items {get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct) => Items = await db.SystemLogs.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).Select(x=>new Row(x.CreatedAt,x.Level,x.Type,x.UserEmail,x.Message,x.MetadataJson)).ToListAsync(ct);
 public sealed record Row(DateTime CreatedAt,string Level,string Category,string? User,string Message,string? TechnicalDetails);
}
