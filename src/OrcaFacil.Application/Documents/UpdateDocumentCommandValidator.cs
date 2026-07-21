using FluentValidation;

namespace OrcaFacil.Application.Documents;

public class UpdateDocumentCommandValidator : AbstractValidator<UpdateDocumentCommand>
{
    public UpdateDocumentCommandValidator()
    {
        RuleFor(x => x.ClientName).NotEmpty().WithMessage("Cliente é obrigatório.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Informe pelo menos um item.");
    }
}
