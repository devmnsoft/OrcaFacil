using FluentValidation;
using OrcaFacil.Application.Auth;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Validation;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.AccountType).IsInEnum();
        RuleFor(x => x.DocumentNumber).NotEmpty().Must((command, document) =>
            BrazilianDocument.HasValidCheckDigits(
                command.AccountType == PersonType.Company ? BrazilianDocumentType.CNPJ : BrazilianDocumentType.CPF,
                document)).WithMessage("Informe um CPF ou CNPJ válido.");
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(180);
        RuleFor(x => x.LegalName).NotEmpty().When(x => x.AccountType == PersonType.Company);
        RuleFor(x => x.ResponsibleName).NotEmpty().When(x => x.AccountType == PersonType.Company);
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty().Length(2);
        RuleFor(x => x.PostalCode).NotEmpty().When(x => x.AccountType == PersonType.Company);
        RuleFor(x => x.Street).NotEmpty().When(x => x.AccountType == PersonType.Company);
        RuleFor(x => x.StreetNumber).NotEmpty().When(x => x.AccountType == PersonType.Company);
        RuleFor(x => x.District).NotEmpty().When(x => x.AccountType == PersonType.Company);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.AcceptTerms).Equal(true);
        RuleFor(x => x.AcceptPrivacy).Equal(true);
    }
}
