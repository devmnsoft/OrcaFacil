using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.Pages.Onboarding;
using System.ComponentModel.DataAnnotations;
[Authorize] public sealed class ClientModel(IOnboardingApplicationService onboarding):PageModel { [BindProperty] public InputModel Input{get;set;}=new(); public sealed class InputModel { [Required] public string Name{get;set;}=""; public PersonType PersonType{get;set;} public string? Document{get;set;} public string? Phone{get;set;} public string? WhatsApp{get;set;} [EmailAddress] public string? Email{get;set;} public string? City{get;set;} } public void OnGet(){} public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid)return Page();var r=await onboarding.CreateClientAsync(new(Input.Name,Input.PersonType,Input.Document,Input.Phone,Input.WhatsApp,Input.Email,Input.City),ct);if(!r.Succeeded){foreach(var e in r.Errors??[])ModelState.AddModelError(e.Field,e.Message);ModelState.AddModelError("",r.Message!);return Page();}return RedirectToPage("Service");} public async Task<IActionResult> OnPostSkipAsync(CancellationToken ct){var r=await onboarding.SkipAsync(OnboardingStep.FirstClient,ct);if(!r.Succeeded)ModelState.AddModelError("",r.Message!);return r.Succeeded?RedirectToPage("Service"):Page();}}