using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Auth;
using OrcaFacil.Web.Extensions;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<LoginModel> _logger;
    private readonly OrcaFacilDbContext _db;
    public LoginModel(AuthService authService, ILogger<LoginModel> logger, OrcaFacilDbContext db) { _authService = authService; _logger = logger; _db = db; }
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
        var user = result.Value;
        var claims = new List<Claim> { new("sub", user.Id.ToString()), new(ClaimTypes.NameIdentifier, user.Id.ToString()), new("name", user.Name), new(ClaimTypes.Name, user.Name), new("email", user.Email), new(ClaimTypes.Email, user.Email), new("role", user.Role), new(ClaimTypes.Role, user.Role), new("plan", user.Plan), new("session_version", user.SessionVersion.ToString()) };
        var memberships = await (from member in _db.AccountMembers.AsNoTracking()
                                 join account in _db.BusinessAccounts.AsNoTracking() on member.AccountId equals account.Id
                                 where member.UserId == user.Id && !member.IsDeleted && member.Status == AccountMemberStatus.Active &&
                                       !account.IsDeleted && account.Status == AccountStatus.Active
                                 select new { Member = member, Account = account }).Take(2).ToListAsync(ct);
        if (memberships.Count == 1)
        {
            var selected = memberships[0];
            claims.Add(new("account_id", selected.Account.Id.ToString()));
            claims.Add(new("account_member_id", selected.Member.Id.ToString()));
            claims.Add(new("account_role", selected.Member.RoleCode));
            claims.Add(new("account_status", selected.Account.Status.ToString()));
        }
        await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
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
