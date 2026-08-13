namespace OrcaFacil.Application.Pricing;

public sealed record PricingItem(decimal Quantity, decimal UnitPrice, decimal UnitCost, decimal Discount = 0, decimal DesiredMarginPercentage = 0);
public sealed record PricingPolicy(decimal MaximumDiscountPercentage, decimal MinimumMarginPercentage, bool CanViewCosts);
public sealed record PricingAlert(string Code, string Message, string Severity);
public sealed record PricingResult(decimal Subtotal, decimal Discount, decimal Surcharge, decimal Total,
    decimal? EstimatedCost, decimal? GrossMargin, decimal? MarginPercentage,
    IReadOnlyList<PricingAlert> Alerts, IReadOnlyList<decimal> SuggestedUnitPrices);

public interface IPricingEngineService
{
    PricingResult Calculate(IReadOnlyList<PricingItem> items, decimal documentDiscount, decimal surcharge, PricingPolicy policy);
}

public sealed class PricingEngineService : IPricingEngineService
{
    public PricingResult Calculate(IReadOnlyList<PricingItem> items, decimal documentDiscount, decimal surcharge, PricingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Any(x => x.Quantity < 0 || x.UnitPrice < 0 || x.UnitCost < 0 || x.Discount < 0))
            throw new ArgumentOutOfRangeException(nameof(items), "Quantidade, preço, custo e desconto não podem ser negativos.");

        var subtotal = Money(items.Sum(x => x.Quantity * x.UnitPrice));
        var itemDiscount = Money(items.Sum(x => Math.Min(x.Quantity * x.UnitPrice, x.Discount)));
        documentDiscount = Money(Math.Max(0, documentDiscount));
        surcharge = Money(Math.Max(0, surcharge));
        var discount = Money(Math.Min(subtotal, itemDiscount + documentDiscount));
        var total = Money(Math.Max(0, subtotal - discount + surcharge));
        var cost = Money(items.Sum(x => x.Quantity * x.UnitCost));
        var margin = Money(total - cost);
        var marginPercentage = total == 0 ? 0 : Math.Round(margin / total * 100, 2, MidpointRounding.AwayFromZero);
        var discountPercentage = subtotal == 0 ? 0 : discount / subtotal * 100;
        var alerts = new List<PricingAlert>();

        if (items.Any(x => x.UnitPrice < x.UnitCost)) alerts.Add(new("below_cost", "Este item está abaixo do custo estimado.", "danger"));
        if (marginPercentage < policy.MinimumMarginPercentage) alerts.Add(new("low_margin", "A margem deste orçamento está abaixo da margem mínima configurada.", "warning"));
        if (discountPercentage > policy.MaximumDiscountPercentage) alerts.Add(new("discount_limit", "O desconto aplicado está acima do permitido para seu perfil.", "danger"));

        var suggestions = items.Select(x => x.DesiredMarginPercentage is >= 100 ? x.UnitPrice :
            Money(x.UnitCost / (1 - Math.Clamp(x.DesiredMarginPercentage, 0, 99.99m) / 100))).ToArray();
        return new(subtotal, discount, surcharge, total, policy.CanViewCosts ? cost : null,
            policy.CanViewCosts ? margin : null, policy.CanViewCosts ? marginPercentage : null, alerts, suggestions);
    }

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
