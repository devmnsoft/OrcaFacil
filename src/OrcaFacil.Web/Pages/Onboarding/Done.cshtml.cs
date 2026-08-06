using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Web.Pages.Onboarding;
[Authorize] public sealed class DoneModel(IOnboardingApplicationService onboarding):PageModel { public async Task<IActionResult> OnGetAsync(CancellationToken ct){var s=await onboarding.GetAsync(ct);return s.Succeeded?Page():Forbid();}}