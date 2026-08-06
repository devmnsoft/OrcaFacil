using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.Pages.Onboarding;
using System.ComponentModel.DataAnnotations;
[Authorize] public sealed class ServiceModel(IOnboardingApplicationService onboarding):PageModel { [BindProperty] public InputModel Input{get;set;}=new(){Name="Instalação de rede Wi-Fi",Unit="service",Price=850}; public sealed class InputModel { [Required] public string Name{get;set;}=""; public string? Category{get;set;} [Required] public string Unit{get;set;}="service"; [Range(0,double.MaxValue)] public decimal Price{get;set;} [Range(0,double.MaxValue)] public decimal? Cost{get;set;} [Range(1,100000)] public int? DurationMinutes{get;set;} public string? Description{get;set;} } public void OnGet(){} public async Task<IActionResult> OnPostAsync(CancellationToken ct){if(!ModelState.IsValid)return Page();var r=await onboarding.CreateServiceAsync(new(Input.Name,Input.Category,Input.Unit,Input.Price,Input.Cost,Input.DurationMinutes,Input.Description),ct);if(!r.Succeeded){foreach(var e in r.Errors??[])ModelState.AddModelError(e.Field,e.Message);ModelState.AddModelError("",r.Message!);return Page();}return RedirectToPage("Budget");} public async Task<IActionResult> OnPostSkipAsync(CancellationToken ct){var r=await onboarding.SkipAsync(OnboardingStep.FirstService,ct);if(!r.Succeeded)ModelState.AddModelError("",r.Message!);return r.Succeeded?RedirectToPage("Budget"):Page();}}