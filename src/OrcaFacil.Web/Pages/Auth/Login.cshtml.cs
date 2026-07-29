using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Auth;
using OrcaFacil.Web.Extensions;
using OrcaFacil.Web.Services;

namespace OrcaFacil.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<LoginModel> _logger;
    private readonly IUserSignInService _signIn;
    public LoginModel(AuthService authService, ILogger<LoginModel> logger, IUserSignInService signIn) { _authService = authService; _logger = logger; _signIn = signIn; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public record InputModel { [Required, EmailAddress] public string Email { get; set; } = string.Empty; [Required] public string Password { get; set; } = string.Empty; }
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Dashboard/Index") : Page();
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData.Warning("Informe e-mail e senha para entrar."); return Page(); }
        try
        {
            var result = await _authService.LoginAsync(new LoginUserCommand(Input.Email, Input.Password, HttpContext.TraceIdentifier), ct);
            if (!result.Succeeded || result.Value is null) { ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível entrar."); TempData.Error(result.Error ?? "Não foi possível entrar."); _logger.LogWarning("USER_LOGIN_FAILED_WEB CorrelationId {CorrelationId}", HttpContext.TraceIdentifier); return Page(); }
            await _signIn.SignInAsync(HttpContext, result.Value, cancellationToken: ct);
            TempData.Success("Login realizado com sucesso.");
            return RedirectToPage("/Dashboard/Index");
        }
        catch (Exception ex) when (ex.IsPostgresInvalidPassword())
        {
            _logger.LogError("POSTGRES_AUTH_FAILED_LOGIN CorrelationId {CorrelationId}", HttpContext.TraceIdentifier);
            ModelState.AddModelError(string.Empty, "Não foi possível entrar agora porque o serviço de dados está temporariamente indisponível.");
            TempData.Error("Não foi possível entrar agora porque o serviço de dados está temporariamente indisponível.");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LOGIN_UNEXPECTED_ERROR CorrelationId {CorrelationId} Operation {Operation}", HttpContext.TraceIdentifier, "Login");
            ModelState.AddModelError(string.Empty, "Não foi possível concluir seu acesso agora. Tente novamente em instantes ou fale com a MNSOFT.");
            TempData.Error("Não foi possível concluir seu acesso agora. Tente novamente em instantes ou fale com a MNSOFT.");
            return Page();
        }
    }
}
