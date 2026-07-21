using FluentValidation;
using OrcaFacil.Application.Documents;

namespace OrcaFacil.Application.Validation;

public class ApprovePublicQuoteCommandValidator : AbstractValidator<ApprovePublicQuoteCommand>
{
    public ApprovePublicQuoteCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MinimumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AcceptedTerms).Equal(true);
    }
}
