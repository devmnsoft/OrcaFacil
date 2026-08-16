using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
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
    private readonly OrcaFacil.Persistence.OrcaFacilDbContext _db;
    public LoginModel(AuthService authService, ILogger<LoginModel> logger, IUserSignInService signIn, OrcaFacil.Persistence.OrcaFacilDbContext db) { _authService = authService; _logger = logger; _signIn = signIn; _db = db; }
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
            // Validate the post-login dependency before emitting the authentication cookie. This
            // avoids leaving a valid identity behind when an older database cannot run onboarding.
            var configured = await _db.AccountOnboardingStates.AsNoTracking().AnyAsync(x => x.UserId == result.Value.Id && x.CompletedAt != null && !x.IsDeleted, ct);
            await _signIn.SignInAsync(HttpContext, result.Value, cancellationToken: ct);
            TempData.Success("Login realizado com sucesso.");
            return RedirectToPage(configured ? "/Dashboard/Index" : "/Onboarding/Index");
        }
        catch (Exception ex) when (ex.IsPostgresInvalidPassword())
        {
            _logger.LogError("POSTGRES_AUTH_FAILED_LOGIN CorrelationId {CorrelationId}", HttpContext.TraceIdentifier);
            ModelState.AddModelError(string.Empty, "Não foi possível entrar agora porque o serviço de dados está temporariamente indisponível.");
            TempData.Error("Não foi possível entrar agora porque o serviço de dados está temporariamente indisponível.");
            return Page();
        }
        catch (Exception ex) when (ex.IsPostgresUndefinedColumn() || ex.IsPostgresUndefinedTable())
        {
            // Defensive cleanup also covers failures after a custom sign-in implementation has
            // started writing the response.
            await HttpContext.SignOutAsync();
            var sqlState = ex.IsPostgresUndefinedTable() ? "42P01" : "42703";
            _logger.LogError(ex, "LOGIN_SCHEMA_OUTDATED CorrelationId {CorrelationId} SqlState {SqlState}", HttpContext.TraceIdentifier, sqlState);
            const string message = "O login foi validado, mas o banco de dados está desatualizado para o onboarding. Execute a atualização do schema e tente novamente.";
            ModelState.AddModelError(string.Empty, message);
            TempData.Error(message);
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
