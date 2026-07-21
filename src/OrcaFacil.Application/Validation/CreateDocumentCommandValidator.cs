using FluentValidation;
using OrcaFacil.Application.Documents;

namespace OrcaFacil.Application.Validation;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(40);
        RuleFor(x => x.ClientName).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
    }
}
