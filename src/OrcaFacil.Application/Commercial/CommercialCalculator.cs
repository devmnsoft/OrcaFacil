namespace OrcaFacil.Application.Commercial;

public sealed record CommercialLine(decimal Quantity, decimal UnitPrice, decimal Discount = 0, decimal Surcharge = 0);
public sealed record CommercialTotals(decimal Subtotal, decimal Discount, decimal Surcharge, decimal Total);

public static class CommercialCalculator
{
    public static CommercialTotals Calculate(IEnumerable<CommercialLine> lines)
    {
        var materialized = lines?.ToArray() ?? throw new ArgumentNullException(nameof(lines));
        if (materialized.Length == 0) throw new ArgumentException("O orçamento precisa ter ao menos um item.", nameof(lines));
        if (materialized.Any(x => x.Quantity <= 0 || x.UnitPrice < 0 || x.Discount < 0 || x.Surcharge < 0))
            throw new ArgumentException("Quantidade e valores devem ser válidos e não negativos.", nameof(lines));

        var subtotal = Round(materialized.Sum(x => x.Quantity * x.UnitPrice));
        var discount = Round(materialized.Sum(x => x.Discount));
        var surcharge = Round(materialized.Sum(x => x.Surcharge));
        if (discount > subtotal + surcharge) throw new ArgumentException("O desconto não pode superar o valor dos itens.", nameof(lines));
        return new(subtotal, discount, surcharge, Round(subtotal - discount + surcharge));
    }

    public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
