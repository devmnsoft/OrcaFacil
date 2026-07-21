using FluentValidation;

namespace OrcaFacil.Application.Documents;

public class CreateReceiptCommandValidator : AbstractValidator<CreateReceiptCommand>
{
    public CreateReceiptCommandValidator()
    {
        RuleFor(x => x.ClientName).NotEmpty().WithMessage("Cliente é obrigatório.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Informe pelo menos um item ou descrição.");
    }
}
