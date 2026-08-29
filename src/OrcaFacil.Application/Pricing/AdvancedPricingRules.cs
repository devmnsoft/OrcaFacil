using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Pricing;

public static class AdvancedPricingRules
{
    public static ServicePriceTable SelectTable(IEnumerable<ServicePriceTable> tables, Guid accountId,
        DateOnly onDate, Guid? customerId = null, Guid? segmentId = null, Guid? contractId = null)
    {
        var applicable = tables.Where(x => x.AccountId == accountId && x.IsActive && x.ValidFrom <= onDate &&
            (x.ValidUntil is null || x.ValidUntil >= onDate) && ScopeMatches(x, customerId, segmentId, contractId));

        return applicable.OrderByDescending(x => Priority(x.Scope)).ThenByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.Version).FirstOrDefault()
            ?? throw new InvalidOperationException("Nenhuma tabela de preço vigente é aplicável.");
    }

    public static void Validate(ServicePriceTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (string.IsNullOrWhiteSpace(table.Name)) throw new ArgumentException("A tabela precisa ter nome.");
        if (table.ValidUntil < table.ValidFrom) throw new ArgumentException("A vigência final não pode ser anterior à inicial.");
    }

    public static bool RequiresApproval(decimal subtotal, decimal discount, decimal price, decimal cost,
        PricingDiscountPolicy discountPolicy, PricingMarginPolicy marginPolicy)
    {
        if (subtotal < 0 || discount < 0 || cost < 0 || price < 0) throw new ArgumentOutOfRangeException(nameof(subtotal));
        if (discount > subtotal) throw new ArgumentException("O desconto não pode tornar o total negativo.");
        var percentage = subtotal == 0 ? 0 : discount / subtotal * 100;
        var margin = price == 0 ? 0 : (price - cost) / price * 100;
        return discount > discountPolicy.MaximumAmountWithoutApproval ||
               percentage > discountPolicy.MaximumPercentageWithoutApproval ||
               marginPolicy.RequiresApprovalBelowMinimum && margin < marginPolicy.MinimumMarginPercentage;
    }

    public static PricingQuoteSnapshot Snapshot(Guid accountId, Guid quoteId, int sequence, decimal basePrice,
        decimal discount, decimal cost, Guid userId, string payloadJson)
    {
        if (discount > basePrice) throw new ArgumentException("O desconto não pode tornar o total negativo.");
        var total = decimal.Round(basePrice - discount, 2, MidpointRounding.AwayFromZero);
        var margin = total == 0 ? 0 : decimal.Round((total - cost) / total * 100, 2, MidpointRounding.AwayFromZero);
        return new() { AccountId = accountId, QuoteId = quoteId, Sequence = sequence, PayloadJson = payloadJson,
            BasePrice = basePrice, Discount = discount, TotalCost = cost, TotalPrice = total,
            MarginPercentage = margin, CreatedByUserId = userId };
    }

    public static void Decide(PricingApprovalEvent approval, Guid actorId, bool approve, string justification)
    {
        if (approval.Status != CommercialApprovalStatus.Pending) throw new InvalidOperationException("A exceção já foi decidida.");
        if (actorId != approval.ApproverUserId) throw new UnauthorizedAccessException("Somente o responsável pode decidir esta exceção.");
        if (actorId == approval.RequestedByUserId) throw new UnauthorizedAccessException("O solicitante não pode aprovar a própria exceção.");
        if (string.IsNullOrWhiteSpace(justification)) throw new ArgumentException("A decisão exige justificativa.");
        approval.Status = approve ? CommercialApprovalStatus.Approved : CommercialApprovalStatus.Rejected;
        approval.DecisionReason = justification.Trim();
        approval.DecidedAt = DateTime.UtcNow;
    }

    private static bool ScopeMatches(ServicePriceTable x, Guid? customer, Guid? segment, Guid? contract) => x.Scope switch
    {
        PriceTableScope.Account => true,
        PriceTableScope.Customer => customer.HasValue && x.CustomerId == customer,
        PriceTableScope.Segment => segment.HasValue && x.SegmentId == segment,
        PriceTableScope.Contract => contract.HasValue && x.ContractId == contract,
        _ => false
    };

    private static int Priority(PriceTableScope scope) => scope switch
    { PriceTableScope.Contract => 60, PriceTableScope.Customer => 50, PriceTableScope.Segment => 40, PriceTableScope.Account => 10, _ => 0 };
}
