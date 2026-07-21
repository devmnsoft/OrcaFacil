using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Auth;

namespace OrcaFacil.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<LoginModel> _logger;
    public LoginModel(AuthService authService, ILogger<LoginModel> logger) { _authService = authService; _logger = logger; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public record InputModel { [Required, EmailAddress] public string Email { get; set; } = string.Empty; [Required] public string Password { get; set; } = string.Empty; }
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Dashboard/Index") : Page();
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var result = await _authService.LoginAsync(new LoginUserCommand(Input.Email, Input.Password), ct);
        if (!result.Succeeded || result.Value is null) { ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível entrar."); _logger.LogWarning("USER_LOGIN_FAILED_WEB {Email}", Input.Email); return Page(); }
        var user = result.Value;
        var claims = new[] { new Claim("sub", user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("name", user.Name), new Claim(ClaimTypes.Name, user.Name), new Claim("email", user.Email), new Claim(ClaimTypes.Email, user.Email), new Claim("role", user.Role), new Claim(ClaimTypes.Role, user.Role), new Claim("plan", user.Plan) };
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
        return RedirectToPage("/Dashboard/Index");
    }
}
