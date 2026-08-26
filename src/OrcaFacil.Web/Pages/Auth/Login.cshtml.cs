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
    private readonly IAccountSelectionService _accountSelection;
    private readonly OrcaFacil.Persistence.OrcaFacilDbContext _db;
    public LoginModel(AuthService authService, ILogger<LoginModel> logger, IUserSignInService signIn,
        IAccountSelectionService accountSelection, OrcaFacil.Persistence.OrcaFacilDbContext db)
    {
        _authService = authService;
        _logger = logger;
        _signIn = signIn;
        _accountSelection = accountSelection;
        _db = db;
    }
    [BindProperty] public InputModel Input { get; set; } = new();
    public record InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        // Honeypot: the field is visually hidden and must remain empty. This adds a lightweight,
        // accessible bot check without forcing legitimate users through an external CAPTCHA.
        public string? Website { get; set; }
    }
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Dashboard/Index") : Page();
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(Input.Website))
        {
            _logger.LogWarning("LOGIN_BOT_CHALLENGE_FAILED CorrelationId {CorrelationId}", HttpContext.TraceIdentifier);
            ModelState.AddModelError(string.Empty, "Não foi possível validar este acesso. Atualize a página e tente novamente.");
            return Page();
        }

        if (!ModelState.IsValid) { TempData.Warning("Informe e-mail e senha para entrar."); return Page(); }
        try
        {
            var result = await _authService.LoginAsync(new LoginUserCommand(Input.Email, Input.Password, HttpContext.TraceIdentifier), ct);
            if (!result.Succeeded || result.Value is null) { ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível entrar."); TempData.Error(result.Error ?? "Não foi possível entrar."); _logger.LogWarning("USER_LOGIN_FAILED_WEB CorrelationId {CorrelationId}", HttpContext.TraceIdentifier); return Page(); }
            // Select the same account used by the cookie and initialize onboarding before emitting
            // it. Besides validating the schema, the upsert closes the first-login race safely and
            // reactivates a soft-deleted state so its unique key cannot strand the user in a loop.
            var (account, _) = await _accountSelection.SelectAsync(result.Value.Id, null, ct);
            if (account is null)
            {
                const string noAccountMessage = "Seu acesso foi validado, mas não há uma conta ativa vinculada. Fale com o suporte MNSOFT.";
                _logger.LogWarning("LOGIN_ACTIVE_ACCOUNT_MISSING UserId {UserId} CorrelationId {CorrelationId}",
                    result.Value.Id, HttpContext.TraceIdentifier);
                ModelState.AddModelError(string.Empty, noAccountMessage);
                TempData.Error(noAccountMessage);
                return Page();
            }

            var now = DateTime.UtcNow;
            const string initialStep = "Welcome";
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO orcafacil.account_onboarding_states
                    (id, account_id, user_id, current_step, last_seen_at, created_at, is_deleted)
                VALUES
                    ({Guid.NewGuid()}, {account.AccountId}, {result.Value.Id}, {initialStep}, {now}, {now}, false)
                ON CONFLICT (account_id, user_id) DO UPDATE
                SET is_deleted = false,
                    current_step = CASE
                        WHEN account_onboarding_states.is_deleted THEN {initialStep}
                        ELSE account_onboarding_states.current_step
                    END,
                    last_seen_at = {now},
                    updated_at = {now}
                """, ct);
            var configured = await _db.AccountOnboardingStates.AsNoTracking().AnyAsync(x =>
                x.AccountId == account.AccountId && x.UserId == result.Value.Id &&
                x.CompletedAt != null && !x.IsDeleted, ct);

            await _signIn.SignInAsync(HttpContext, result.Value, account.AccountId, cancellationToken: ct);
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
            // Do not emit provider messages here: they may contain server or connection metadata.
            _logger.LogError("LOGIN_UNEXPECTED_ERROR CorrelationId {CorrelationId} Operation {Operation} FailureType {FailureType}",
                HttpContext.TraceIdentifier, "Login", ex.GetType().Name);
            ModelState.AddModelError(string.Empty, "Não foi possível concluir seu acesso agora. Tente novamente em instantes ou fale com a MNSOFT.");
            TempData.Error("Não foi possível concluir seu acesso agora. Tente novamente em instantes ou fale com a MNSOFT.");
            return Page();
        }
    }
}
