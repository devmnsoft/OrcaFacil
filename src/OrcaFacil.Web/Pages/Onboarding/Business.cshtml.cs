using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.Pages.Onboarding;
using System.ComponentModel.DataAnnotations;
[Authorize] public sealed class BusinessModel(IOnboardingApplicationService onboarding):PageModel { [BindProperty] public InputModel Input{get;set;}=new(); public sealed class InputModel { [Required] public PersonType PersonType{get;set;} [Required] public string Name{get;set;}=""; [Required] public string Document{get;set;}=""; [Required] public string Phone{get;set;}=""; public string? WhatsApp{get;set;} [Required,EmailAddress] public string Email{get;set;}=""; [Required] public string City{get;set;}=""; [Required,StringLength(2,MinimumLength=2)] public string State{get;set;}=""; } public void OnGet(){} public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid)return Page();var r=await onboarding.SaveBusinessAsync(new(Input.PersonType,Input.Name,Input.Document,Input.Phone,Input.WhatsApp,Input.Email,Input.City,Input.State),ct);if(!r.Succeeded){foreach(var e in r.Errors??[])ModelState.AddModelError(e.Field,e.Message);ModelState.AddModelError("",r.Message!);return Page();}return RedirectToPage("DocumentIdentity");}}