using FluentValidation;

namespace OrcaFacil.Application.Validation;

public record SaveIssuerProfileCommand(Guid UserId, string BusinessName, string? DocumentNumber, string? Phone, string? Email, string? Address, string? City, string? PixKey);

public class SaveIssuerProfileCommandValidator : AbstractValidator<SaveIssuerProfileCommand>
{
    public SaveIssuerProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
