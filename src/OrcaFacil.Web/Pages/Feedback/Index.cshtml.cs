using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
namespace OrcaFacil.Web.Pages.Feedback;
[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current) : PageModel
{
 [BindProperty,Required,RegularExpression("Gostei|Não gostei|Algo está confuso|Encontrei um erro|Sugestão")] public string Rating {get;set;}=string.Empty;
 [BindProperty,StringLength(2000)] public string? Message {get;set;}
 [BindProperty,Required,StringLength(500)] public string PageUrl {get;set;}="/";
 [BindProperty,StringLength(100)] public string? CorrelationId {get;set;}
 public void OnGet()=>PageUrl=Request.Headers.Referer.FirstOrDefault()??"/";
 public async Task<IActionResult> OnPostAsync(CancellationToken ct)
 {
  if(current.AccountId is null)return Forbid();
  if(!ModelState.IsValid)return Page();
  db.UserFeedback.Add(new UserFeedback{AccountId=current.AccountId,UserId=current.UserId,PageUrl=SafePage(PageUrl),Rating=Rating,Message=Message?.Trim(),BrowserInfo=Safe(Request.Headers.UserAgent.FirstOrDefault(),500),CorrelationId=Safe(CorrelationId??HttpContext.TraceIdentifier,100)});
  await db.SaveChangesAsync(ct); TempData["Success"]="Obrigado. Seu feedback foi enviado à equipe."; return RedirectToPage();
 }
 static string SafePage(string value) { if(!Uri.TryCreate(value,UriKind.RelativeOrAbsolute,out var uri)) return "/"; var page=(uri.IsAbsoluteUri?uri.AbsolutePath:uri.ToString()).Split('?', '#')[0]; return page[..Math.Min(page.Length,500)]; }
 static string? Safe(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:value[..Math.Min(value.Length,max)];
}
