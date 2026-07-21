using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Auth;
using OrcaFacil.Web.Extensions;

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
        if (!ModelState.IsValid) { TempData.Warning("Informe e-mail e senha para entrar."); return Page(); }
        try
        {
            var result = await _authService.LoginAsync(new LoginUserCommand(Input.Email, Input.Password), ct);
            if (!result.Succeeded || result.Value is null) { ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível entrar."); TempData.Error(result.Error ?? "Não foi possível entrar."); _logger.LogWarning("USER_LOGIN_FAILED_WEB {Email}", Input.Email); return Page(); }
        var user = result.Value;
        var claims = new[] { new Claim("sub", user.Id.ToString()), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("name", user.Name), new Claim(ClaimTypes.Name, user.Name), new Claim("email", user.Email), new Claim(ClaimTypes.Email, user.Email), new Claim("role", user.Role), new Claim(ClaimTypes.Role, user.Role), new Claim("plan", user.Plan) };
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
            TempData.Success("Login realizado com sucesso.");
            return RedirectToPage("/Dashboard/Index");
        }
        catch (Exception ex) when (ex.IsPostgresInvalidPassword())
        {
            _logger.LogError(ex, "POSTGRES_AUTH_FAILED_LOGIN SqlState 28P01 for {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "Não foi possível conectar ao banco de dados. Verifique a senha do usuário orcafacil_user nas configurações do sistema.");
            TempData.Error("Não foi possível entrar agora. A conexão com o banco de dados precisa ser verificada.");
            return Page();
        }
    }
}
