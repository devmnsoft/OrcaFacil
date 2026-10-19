using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.Profile;
using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Web.Pages.Profile;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly ICurrentAccountService _account;
    private readonly IPlanAccessService _plans;
    private readonly ProfileService _service;

    public IndexModel(ICurrentUserService current, ICurrentAccountService account, IPlanAccessService plans, ProfileService service)
    {
        _current = current;
        _account = account;
        _plans = plans;
        _service = service;
    }

    [BindProperty]
    public SaveIssuerProfileCommand Input { get; set; } = new(Guid.Empty, "", null, null, null, null, null, null, null);

    public async Task OnGetAsync(CancellationToken ct)
    {
        var p = await _service.GetAsync(new(_current.UserId), ct);
        Input = new(_current.UserId, p?.BusinessName ?? "", p?.DocumentNumber, p?.Phone, p?.Email, p?.Address, p?.City, p?.PixKey, p?.LogoPath);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Input = Input with { UserId = _current.UserId };

        // Gate de plano: CustomBrandingEnabled — logo próprio exige plano Pro
        if (!string.IsNullOrEmpty(Input.LogoPath))
        {
            var accountId = _account.AccountId ?? Guid.Empty;
            var brandingAllowed = await _plans.CanUseAsync(accountId, PlanFeatureCodes.CustomBrandingEnabled, ct);
            if (!brandingAllowed.IsAllowed)
            {
                ModelState.AddModelError("", "Logo próprio está disponível a partir do plano Profissional. Conheça os planos.");
                return Page();
            }
        }

        var r = await _service.SaveAsync(Input, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Erro ao salvar.");
            return Page();
        }
        TempData["Success"] = "Perfil salvo.";
        return RedirectToPage();
    }
}
