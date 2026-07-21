using FluentValidation;
using OrcaFacil.Application.Auth;

namespace OrcaFacil.Application.Validation;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(160);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.AcceptTerms).Equal(true);
        RuleFor(x => x.AcceptPrivacy).Equal(true);
    }
}
