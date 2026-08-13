using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.Pages.Onboarding;
[Authorize] public sealed class IndexModel(IOnboardingApplicationService onboarding):PageModel { public OnboardingStateView State{get;private set;}=null!; public async Task<IActionResult> OnGetAsync(CancellationToken ct){var r=await onboarding.GetAsync(ct);if(!r.Succeeded)return Forbid();State=r.Value!;return Page();} public async Task<IActionResult> OnPostBeginAsync(CancellationToken ct){await onboarding.BeginAsync(ct);return RedirectToPage("Business");} public async Task<IActionResult> OnPostSkipAsync(CancellationToken ct){await onboarding.SkipAsync(OnboardingStep.Welcome,ct);return RedirectToPage("/Dashboard/Index");}}
