using System.ComponentModel.DataAnnotations; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.RazorPages; using OrcaFacil.Web.Security; using OrcaFacil.Web.Services;
namespace OrcaFacil.Web.Pages.Auth;
/// <summary>Aceite público do convite de equipe: dados do convite vem do token cru (só hash é persistido); no POST o aceite é único e o login já sai feito.</summary>
[AllowAnonymous] public sealed class AcceptInviteModel(IAccountInvitationService invitations,IUserSignInService signIn):PageModel
{ [BindProperty(SupportsGet=true)] public string Token{get;set;}=""; [BindProperty,Required,MinLength(10)] public string Password{get;set;}=""; [BindProperty,Required,Compare(nameof(Password))] public string Confirmation{get;set;}=""; [BindProperty] public bool AcceptTerms{get;set;} public bool Available{get;set;} public string FullName{get;set;}=""; public string Email{get;set;}=""; public string AccountName{get;set;}=""; public string RoleCode{get;set;}=""; public string? Error{get;set;}
 public async Task OnGetAsync(CancellationToken ct)=>await LoadAsync(ct);
 public async Task<IActionResult> OnPostAsync(CancellationToken ct)
 {
  await LoadAsync(ct);
  if(!Available)return Page();
  if(!ModelState.IsValid)return Page();
  var result=await invitations.AcceptAsync(Token,Password,AcceptTerms,HttpContext.TraceIdentifier,ct);
  if(!result.Succeeded){Available=false;Error=result.Error;return Page();}
  await signIn.SignInAsync(HttpContext,result.Value!,cancellationToken:ct);
  return RedirectToPage("/Onboarding/Index");
 }
 private async Task LoadAsync(CancellationToken ct)
 {var view=await invitations.FindPendingAsync(Token,ct);if(view.Succeeded&&view.Value is not null){Available=true;FullName=view.Value.FullName;Email=view.Value.Email;AccountName=view.Value.AccountName;RoleCode=view.Value.RoleCode;}else{Available=false;Error=Error??view.Error;}}
}
