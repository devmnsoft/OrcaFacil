using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.Pages.Onboarding;
using System.ComponentModel.DataAnnotations;
[Authorize] public sealed class DocumentIdentityModel(IOnboardingApplicationService onboarding):PageModel { [BindProperty] public InputModel Input{get;set;}=new(); public sealed class InputModel { [Required] public string DisplayName{get;set;}=""; public string? Phone{get;set;} [EmailAddress] public string? Email{get;set;} public string? City{get;set;} public string? Address{get;set;} public string? PixKey{get;set;} public string? DefaultNote{get;set;} } public void OnGet(){} public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid)return Page();var r=await onboarding.SaveDocumentIdentityAsync(new(Input.DisplayName,Input.Phone,Input.Email,Input.City,Input.Address,Input.PixKey,Input.DefaultNote),ct);if(!r.Succeeded){foreach(var e in r.Errors??[])ModelState.AddModelError(e.Field,e.Message);ModelState.AddModelError("",r.Message!);return Page();}return RedirectToPage("Client");}}