using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Auth;

[Authorize]
public sealed class SelectAccountModel(IAccountSwitcherService switcher) : PageModel
{
    public IReadOnlyList<SwitchableAccount> Accounts { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("user_id"), out var userId)) return Challenge();
        Accounts = await switcher.GetAuthorizedAsync(userId, null, ct);
        if (Accounts.Count == 1) return await SelectAsync(Accounts[0].AccountId, ct);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(Guid accountId, CancellationToken ct) => await SelectAsync(accountId, ct);
    private async Task<IActionResult> SelectAsync(Guid accountId, CancellationToken ct)
    {
        var result = await switcher.SwitchAsync(HttpContext, accountId, ct);
        if (result.Succeeded) return RedirectToPage("/Dashboard/Index");
        ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível abrir esta conta.");
        if (Guid.TryParse(User.FindFirstValue("user_id"), out var userId)) Accounts = await switcher.GetAuthorizedAsync(userId, null, ct);
        return Page();
    }
}
