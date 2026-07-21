using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Auth;
namespace OrcaFacil.Web.Pages.Auth;
[AllowAnonymous]
public class RegisterModel : PageModel { private readonly AuthService _authService; public RegisterModel(AuthService authService)=>_authService=authService; [BindProperty] public InputModel Input { get; set; }=new(); public record InputModel { [Required] public string Name {get;set;}=""; [Required,EmailAddress] public string Email {get;set;}=""; [Required,MinLength(6)] public string Password {get;set;}=""; public bool AcceptTerms {get;set;} public bool AcceptPrivacy {get;set;} } public async Task<IActionResult> OnPostAsync(CancellationToken ct){ if(!ModelState.IsValid)return Page(); var r=await _authService.RegisterAsync(new RegisterUserCommand(Input.Name,Input.Email,Input.Password,Input.AcceptTerms,Input.AcceptPrivacy),ct); if(!r.Succeeded){ModelState.AddModelError(string.Empty,r.Error??"Cadastro inválido.");return Page();} TempData["Success"]="Cadastro criado. Faça login."; return RedirectToPage("/Auth/Login");}}
