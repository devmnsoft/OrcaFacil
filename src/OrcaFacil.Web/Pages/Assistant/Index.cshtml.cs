using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Assistant;
[Authorize]
public sealed class IndexModel(IInternalAssistantService assistant,ICurrentAccountService account):PageModel
{
 [BindProperty] public string Question{get;set;}=""; public AssistantAnswer? Answer{get;private set;}
 public async Task<IActionResult> OnGetAsync(CancellationToken ct)=>await account.HasPermissionAsync(PermissionCodes.AssistantUse,ct)?Page():Forbid();
 public async Task<IActionResult> OnPostAskAsync(CancellationToken ct){if(!await account.HasPermissionAsync(PermissionCodes.AssistantUse,ct))return Forbid();if(string.IsNullOrWhiteSpace(Question)||Question.Length>500){ModelState.AddModelError(nameof(Question),"Digite uma pergunta entre 3 e 500 caracteres.");return Page();}Answer=await assistant.AskAsync(Question,ct);return Page();}
}
