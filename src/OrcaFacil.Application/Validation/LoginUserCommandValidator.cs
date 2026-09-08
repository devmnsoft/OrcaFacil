using FluentValidation;
using OrcaFacil.Application.Auth;

namespace OrcaFacil.Application.Validation;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254)
            .WithName("CPF, CNPJ ou e-mail");
        RuleFor(x => x.Password).NotEmpty();
    }
}
