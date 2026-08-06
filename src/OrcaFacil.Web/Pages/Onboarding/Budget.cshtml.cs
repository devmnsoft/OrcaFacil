using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.Pages.Onboarding;
[Authorize] public sealed class BudgetModel(IOnboardingApplicationService onboarding):PageModel { public OnboardingStateView State{get;private set;}=null!; public async Task OnGetAsync(CancellationToken ct)=>State=(await onboarding.GetAsync(ct)).Value!; public async Task<IActionResult> OnPostAsync(CancellationToken ct){var r=await onboarding.StartBudgetAsync(ct);return RedirectToPage("/Documents/CreateBudget",new{clientId=r.Value});} public async Task<IActionResult> OnPostDoneAsync(CancellationToken ct){var r=await onboarding.CompleteAsync(ct);return r.Succeeded?RedirectToPage("Done"):RedirectToPage("Business");}}