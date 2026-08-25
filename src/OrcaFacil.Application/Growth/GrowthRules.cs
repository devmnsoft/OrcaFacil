namespace OrcaFacil.Application.Growth;

public sealed record GrowthAttributionInput(string? Source, string? Medium, string? Campaign, string? Term,
    string? Content, string? ReferralCode, string? Gclid, string? Fbclid, string? LandingPage, string? Referrer);
public sealed record GrowthAttribution(string Source, string Channel, string? Campaign, string? Term,
    string? Content, string? ReferralCode, string? Gclid, string? Fbclid, string? LandingPage, string? Referrer);

public sealed class GrowthAttributionService
{
    public GrowthAttribution Normalize(GrowthAttributionInput input)
    {
        static string? Clean(string? value, int maximum) => string.IsNullOrWhiteSpace(value)
            ? null : value.Trim()[..Math.Min(value.Trim().Length, maximum)];
        var source = Clean(input.Source, 200) ?? "Direct/Unknown";
        var medium = Clean(input.Medium, 200);
        var channel = medium ?? (source == "Direct/Unknown" ? "Direct/Unknown" : source);
        return new(source, channel, Clean(input.Campaign, 200), Clean(input.Term, 200),
            Clean(input.Content, 200), Clean(input.ReferralCode, 100), Clean(input.Gclid, 300),
            Clean(input.Fbclid, 300), Clean(input.LandingPage, 500), Clean(input.Referrer, 500));
    }
}

public sealed record GrowthLeadSignals(string? Segment, string? CompanySize, string? DesiredPlan,
    string Source, bool RequestedDemo, bool OpenedTrial, bool Replied, bool HasPhone, bool HasCompany,
    bool InterestedInPaidPlan, int EngagementEvents, bool Disqualified = false);
public sealed record GrowthLeadScore(int Score, string Classification, IReadOnlyList<string> Reasons, string NextAction);

public sealed class GrowthLeadScoreService
{
    public GrowthLeadScore Calculate(GrowthLeadSignals value)
    {
        if (value.Disqualified) return new(0, "Desqualificado", ["Lead desqualificado por decisão comercial."], "Registrar o motivo e encerrar o acompanhamento.");
        var score = 0;
        var reasons = new List<string>();
        Add(!string.IsNullOrWhiteSpace(value.Segment), 8, "Segmento informado");
        Add(!string.IsNullOrWhiteSpace(value.CompanySize), 8, "Porte informado");
        Add(value.HasPhone, 8, "Telefone informado");
        Add(value.HasCompany, 8, "Empresa informada");
        Add(value.InterestedInPaidPlan || !string.IsNullOrWhiteSpace(value.DesiredPlan), 15, "Interesse em plano pago");
        Add(value.RequestedDemo, 18, "Demonstração solicitada");
        Add(value.OpenedTrial, 20, "Trial ativado");
        Add(value.Replied, 10, "Contato respondido");
        Add(value.Source != "Direct/Unknown", 3, "Origem identificada");
        var engagement = Math.Min(Math.Max(value.EngagementEvents, 0), 5) * 2;
        if (engagement > 0) { score += engagement; reasons.Add($"{value.EngagementEvents} evento(s) de engajamento"); }
        score = Math.Min(score, 100);
        var classification = score >= 65 ? "Quente" : score >= 35 ? "Morno" : "Frio";
        var next = classification == "Quente" ? "Priorizar contato comercial hoje."
            : classification == "Morno" ? "Agendar follow-up e validar necessidade." : "Nutrir e completar dados de qualificação.";
        return new(score, classification, reasons, next);
        void Add(bool condition, int points, string reason) { if (condition) { score += points; reasons.Add(reason); } }
    }
}

public enum GrowthDiscountType { Percentage, FixedAmount }
public sealed record CouponRule(bool Active, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil,
    GrowthDiscountType Type, decimal Value, int? TotalLimit, int Uses, int PerCustomerLimit, int CustomerUses);
public sealed record CouponResult(bool Applied, decimal Discount, decimal Total, string? Error);

public sealed class CouponService
{
    public CouponResult Apply(CouponRule coupon, decimal subtotal, DateTimeOffset now)
    {
        if (subtotal < 0) return new(false, 0, subtotal, "O subtotal não pode ser negativo.");
        if (!coupon.Active) return new(false, 0, subtotal, "Cupom inativo.");
        if (now < coupon.ValidFrom || now >= coupon.ValidUntil) return new(false, 0, subtotal, "Cupom fora da validade.");
        if (coupon.Value < 0 || coupon.Type == GrowthDiscountType.Percentage && coupon.Value > 100) return new(false, 0, subtotal, "Regra de desconto inválida.");
        if (coupon.TotalLimit is not null && coupon.Uses >= coupon.TotalLimit) return new(false, 0, subtotal, "Limite total atingido.");
        if (coupon.CustomerUses >= coupon.PerCustomerLimit) return new(false, 0, subtotal, "Limite por cliente atingido.");
        var discount = coupon.Type == GrowthDiscountType.Percentage ? subtotal * coupon.Value / 100m : coupon.Value;
        discount = Math.Min(Math.Max(discount, 0), subtotal);
        return new(true, discount, subtotal - discount, null);
    }
}

public sealed class ResellerCommissionService
{
    public decimal Calculate(decimal confirmedPaidAmount, decimal percentage, bool paymentConfirmed, bool resellerActive)
    {
        if (!paymentConfirmed || !resellerActive || confirmedPaidAmount <= 0) return 0;
        if (percentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percentage));
        return decimal.Round(confirmedPaidAmount * percentage / 100m, 2, MidpointRounding.ToEven);
    }
}

public sealed record PaidSubscriptionMetric(decimal MonthlyAmount, bool IsTrial, bool IsActive, bool PaymentReversed);
public sealed class GrowthRevenueService
{
    public decimal CalculateMrr(IEnumerable<PaidSubscriptionMetric> subscriptions) => subscriptions
        .Where(x => x.IsActive && !x.IsTrial && !x.PaymentReversed && x.MonthlyAmount > 0)
        .Sum(x => x.MonthlyAmount);
}
