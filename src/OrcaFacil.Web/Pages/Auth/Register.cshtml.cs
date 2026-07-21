using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Auth;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.Auth;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly AuthService _authService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(AuthService authService, ILogger<RegisterModel> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [BindProperty] public InputModel Input { get; set; } = new();

    public record InputModel
    {
        [Required(ErrorMessage = "Informe seu nome ou empresa.")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "Informe seu e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Crie uma senha."), MinLength(6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;
        [Range(typeof(bool), "true", "true", ErrorMessage = "Aceite os termos para criar sua conta.")]
        public bool AcceptTerms { get; set; }
        [Range(typeof(bool), "true", "true", ErrorMessage = "Aceite a política de privacidade para criar sua conta.")]
        public bool AcceptPrivacy { get; set; }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData.Warning("Revise os campos destacados para concluir seu cadastro.");
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

            TempData.Success("Conta criada com sucesso. Entre para criar seu primeiro orçamento.");
            return RedirectToPage("/Auth/Login");
        }
        catch (Exception ex) when (ex.IsPostgresInvalidPassword())
        {
            _logger.LogError(ex, "POSTGRES_AUTH_FAILED_REGISTER SqlState 28P01 for {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "Não conseguimos concluir seu cadastro agora. Nossa equipe técnica precisa verificar a conexão com o banco de dados.");
            TempData.Error("Não conseguimos concluir seu cadastro agora. Nossa equipe técnica precisa verificar a conexão com o banco de dados.");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REGISTER_UNEXPECTED_ERROR for {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "Não foi possível concluir o cadastro. Tente novamente em instantes ou fale com o suporte MNSOFT.");
            TempData.Error("Não foi possível concluir o cadastro. Tente novamente em instantes ou fale com o suporte MNSOFT.");
            return Page();
        }
    }
}
