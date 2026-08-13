using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.MessageTemplates;
[Authorize]
public sealed class IndexModel(ICommercialAutomationService automation) : PageModel
{
 public IReadOnlyList<MessageTemplateView> Templates { get; private set; }=[];
 public IReadOnlyList<string> Variables => CommercialAutomationService.Variables;
 public async Task OnGetAsync(CancellationToken ct)=>Templates=await automation.GetTemplatesAsync(ct);
 public async Task<IActionResult> OnPostSaveAsync(Guid? id,string name,string channel,string? subject,string body,bool active,CancellationToken ct)
 { var result=await automation.SaveTemplateAsync(id,name,channel,subject,body,active,ct); TempData[result.Ok?"Success":"Error"]=result.Message; return RedirectToPage(); }
}
