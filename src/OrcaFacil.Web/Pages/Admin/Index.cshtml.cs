using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrcaFacil.Web.Pages.Admin;

[Authorize(Policy = "SuperAdminOnly")]
public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Dashboard", new { area = "Admin" });
}
