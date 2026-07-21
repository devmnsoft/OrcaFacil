using FluentValidation;

namespace OrcaFacil.Application.Documents;

public class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(x => x.ClientName).NotEmpty().WithMessage("Cliente é obrigatório.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Informe pelo menos um item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Description).NotEmpty().WithMessage("Descrição do item é obrigatória.");
            item.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
            item.RuleFor(x => x.UnitPrice).GreaterThan(0).WithMessage("Valor unitário deve ser maior que zero.");
        });
    }
}
