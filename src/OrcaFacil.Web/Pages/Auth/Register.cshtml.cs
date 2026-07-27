using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Auth;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.Auth;

[AllowAnonymous]
public sealed class RegisterModel(AuthService authService, ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Escolha como você vai usar o OrçaFácil.")]
        public PersonType? AccountType { get; set; }
        [Required(ErrorMessage = "Informe seu CPF ou CNPJ.")] public string DocumentNumber { get; set; } = string.Empty;
        [MaxLength(180)] public string Name { get; set; } = string.Empty;
        public string? ProfessionalName { get; set; }
        public string? LegalName { get; set; }
        public string? TradeName { get; set; }
        public string? ResponsibleName { get; set; }
        [Required(ErrorMessage = "Informe seu telefone ou WhatsApp.")] public string Phone { get; set; } = string.Empty;
        [Required(ErrorMessage = "Informe um e-mail válido."), EmailAddress(ErrorMessage = "Informe um e-mail válido.")] public string Email { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public string? Street { get; set; }
        public string? StreetNumber { get; set; }
        public string? Complement { get; set; }
        public string? District { get; set; }
        [Required(ErrorMessage = "Informe sua cidade.")] public string City { get; set; } = string.Empty;
        [Required(ErrorMessage = "Informe seu estado."), StringLength(2, MinimumLength = 2)] public string State { get; set; } = string.Empty;
        [Required, MinLength(8, ErrorMessage = "A senha precisa ter pelo menos 8 caracteres.")] public string Password { get; set; } = string.Empty;
        [Required, Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")] public string ConfirmPassword { get; set; } = string.Empty;
        [Range(typeof(bool), "true", "true", ErrorMessage = "Aceite os termos para continuar.")] public bool AcceptTerms { get; set; }
        [Range(typeof(bool), "true", "true", ErrorMessage = "Aceite a política de privacidade para continuar.")] public bool AcceptPrivacy { get; set; }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ApplyConditionalValidation();
        if (!ModelState.IsValid) return InvalidPage();

        var registrationName = Input.AccountType == PersonType.Company ? Input.ResponsibleName! : Input.Name;
        var command = new RegisterUserCommand(Input.AccountType!.Value, Input.DocumentNumber, registrationName,
            Input.ProfessionalName, Input.LegalName, Input.TradeName, Input.ResponsibleName, Input.Phone,
            Input.Email, Input.PostalCode, Input.Street, Input.StreetNumber, Input.Complement, Input.District,
            Input.City, Input.State, Input.Password, Input.AcceptTerms, Input.AcceptPrivacy);
        try
        {
            var result = await authService.RegisterAsync(command, ct);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível concluir o cadastro.");
                return InvalidPage();
            }
            TempData.Success("Conta criada. Vamos preparar seu espaço.");
            return RedirectToPage("/Onboarding/Index");
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            logger.LogError(ex, "REGISTER_FAILED CorrelationId {CorrelationId}", correlationId);
            ModelState.AddModelError(string.Empty, $"Não foi possível concluir seu cadastro agora. Tente novamente em alguns instantes. Se o problema continuar, informe o código {correlationId} ao suporte.");
            return InvalidPage();
        }
    }

    private void ApplyConditionalValidation()
    {
        if (Input.AccountType != PersonType.Company)
        {
            Require(Input.Name, "Input.Name", "Informe seu nome completo.");
            return;
        }
        Require(Input.LegalName, "Input.LegalName", "Informe a razão social.");
        Require(Input.ResponsibleName, "Input.ResponsibleName", "Informe o nome do responsável.");
        Require(Input.PostalCode, "Input.PostalCode", "Informe o CEP.");
        Require(Input.Street, "Input.Street", "Informe a rua.");
        Require(Input.StreetNumber, "Input.StreetNumber", "Informe o número.");
        Require(Input.District, "Input.District", "Informe o bairro.");
    }

    private void Require(string? value, string key, string message) { if (string.IsNullOrWhiteSpace(value)) ModelState.AddModelError(key, message); }
    private PageResult InvalidPage()
    {
        ModelState.Remove("Input.Password"); ModelState.Remove("Input.ConfirmPassword");
        Input.Password = string.Empty; Input.ConfirmPassword = string.Empty;
        TempData.Warning("Revise os campos destacados para concluir seu cadastro.");
        return Page();
    }
}
