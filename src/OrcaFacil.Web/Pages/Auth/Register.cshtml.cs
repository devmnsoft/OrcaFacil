using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Auth;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.Auth;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly AuthService _authService;
    private readonly IDatabaseDiagnosticsService _databaseDiagnostics;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(AuthService authService, IDatabaseDiagnosticsService databaseDiagnostics, ILogger<RegisterModel> logger)
    {
        _authService = authService;
        _databaseDiagnostics = databaseDiagnostics;
        _logger = logger;
    }

    [BindProperty] public InputModel Input { get; set; } = new();

    public record InputModel
    {
        [Required(ErrorMessage = "Informe seu nome ou empresa.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe um e-mail válido."), EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha precisa ter pelo menos 6 caracteres."), MinLength(6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "As senhas não conferem."), Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Aceite os termos para continuar.")]
        public bool AcceptTerms { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Aceite a política de privacidade para continuar.")]
        public bool AcceptPrivacy { get; set; }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData.Warning("Revise os campos destacados para concluir seu cadastro.");
            return Page();
        }

        var correlationId = HttpContext.TraceIdentifier;
        DatabaseDiagnosticsDto diagnostics;
        try
        {
            diagnostics = await _databaseDiagnostics.CheckAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DATABASE_DIAGNOSTIC_FAILED_REGISTER CorrelationId {CorrelationId} Operation {Operation}", correlationId, "Register");
            diagnostics = new(false, false, [], [], null, null, ex.Message);
        }

        if (!diagnostics.CanConnect)
        {
            _logger.LogError("DATABASE_UNAVAILABLE_REGISTER CorrelationId {CorrelationId} Operation {Operation} Error {Error}", correlationId, "Register", diagnostics.Error);
            ModelState.AddModelError(string.Empty, "Não foi possível concluir seu cadastro agora. Tente novamente em instantes ou fale com a MNSOFT.");
            TempData.Error("Não foi possível concluir seu cadastro agora. Tente novamente em instantes ou fale com a MNSOFT.");
            return Page();
        }

        try
        {
            var result = await _authService.RegisterAsync(new RegisterUserCommand(Input.Name, Input.Email, Input.Password, Input.AcceptTerms, Input.AcceptPrivacy), ct);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Cadastro inválido.");
                TempData.Error(result.Error ?? "Não foi possível concluir o cadastro.");
                return Page();
            }

            TempData.Success("Conta criada com sucesso. Agora vamos configurar seus dados.");
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception ex) when (ex.IsPostgresInvalidPassword())
        {
            _logger.LogError(ex, "POSTGRES_AUTH_FAILED_REGISTER SqlState {SqlState} DbUser {DbUser} CorrelationId {CorrelationId} Operation {Operation}", DatabaseExceptionExtensions.InvalidPasswordSqlState, "orcafacil_user", correlationId, "Register");
            ModelState.AddModelError(string.Empty, "Não foi possível concluir seu cadastro agora. Tente novamente em instantes ou fale com a MNSOFT.");
            TempData.Error("Não foi possível concluir seu cadastro agora. Tente novamente em instantes ou fale com a MNSOFT.");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REGISTER_UNEXPECTED_ERROR CorrelationId {CorrelationId} Operation {Operation}", correlationId, "Register");
            ModelState.AddModelError(string.Empty, "Não foi possível concluir seu cadastro agora. Tente novamente em instantes ou fale com a MNSOFT.");
            TempData.Error("Não foi possível concluir seu cadastro agora. Tente novamente em instantes ou fale com a MNSOFT.");
            return Page();
        }
    }
}
