using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Areas.Admin.Pages.SystemLogs;
[Authorize(Policy="PlatformAuditRead")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
 [BindProperty(SupportsGet=true)] public DateTime? From {get;set;}
 [BindProperty(SupportsGet=true)] public DateTime? To {get;set;}
 [BindProperty(SupportsGet=true)] public string? Level {get;set;}
 [BindProperty(SupportsGet=true)] public string? Category {get;set;}
 [BindProperty(SupportsGet=true)] public string? User {get;set;}
 [BindProperty(SupportsGet=true)] public string? Account {get;set;}
 [BindProperty(SupportsGet=true)] public string? CorrelationId {get;set;}
 public IReadOnlyList<Row> Items {get;private set;}=[];
 public async Task OnGetAsync(CancellationToken ct)
 {
  var query=db.SystemLogs.AsNoTracking();
  if(From.HasValue) query=query.Where(x=>x.CreatedAt>=From.Value.ToUniversalTime());
  if(To.HasValue) query=query.Where(x=>x.CreatedAt<To.Value.Date.AddDays(1).ToUniversalTime());
  if(!string.IsNullOrWhiteSpace(Level)) query=query.Where(x=>x.Level==Level.Trim());
  if(!string.IsNullOrWhiteSpace(Category)) query=query.Where(x=>x.Type.Contains(Category.Trim()));
  if(!string.IsNullOrWhiteSpace(User)) query=query.Where(x=>x.UserEmail!=null&&x.UserEmail.Contains(User.Trim()));
  if(!string.IsNullOrWhiteSpace(Account)) query=query.Where(x=>x.MetadataJson!=null&&x.MetadataJson.Contains(Account.Trim()));
  if(!string.IsNullOrWhiteSpace(CorrelationId)) query=query.Where(x=>x.MetadataJson!=null&&x.MetadataJson.Contains(CorrelationId.Trim()));
  Items=await query.OrderByDescending(x=>x.CreatedAt).Take(250).Select(x=>new Row(x.CreatedAt,x.Level,x.Type,x.UserEmail,x.Message,x.MetadataJson)).ToListAsync(ct);
 }
 public sealed record Row(DateTime CreatedAt,string Level,string Category,string? User,string Message,string? TechnicalDetails);
}
