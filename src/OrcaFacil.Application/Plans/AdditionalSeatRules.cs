using OrcaFacil.Domain.Plans;

namespace OrcaFacil.Application.Plans;

/// <summary>
/// Regras puras dos assentos de equipe adicionais pagos (plano Negócio). Sem dependências:
/// teto, elegibilidade, validação de compra e custo — unit-testáveis como as demais regras.
/// </summary>
public static class AdditionalSeatRules
{
    /// <summary>Código estável do addon "usuário adicional" no catálogo (PlanAddon.Code).</summary>
    public const string AddonCode = "additional_users";

    /// <summary>Recurso cujo teto cresce com cada assento contratado (reaproveita o limite de equipe do plano).</summary>
    public const string FeatureCode = PlanFeatureCodes.TeamMembersLimit;

    /// <summary>Plano que pode contratar assentos adicionais.</summary>
    public const string BusinessPlanCode = "BUSINESS";

    /// <summary>Teto de assentos adicionais por conta (Negócio inclui 3; o teto total fica em 8).</summary>
    public const int MaxAdditionalSeats = 5;

    /// <summary>Custo mensal por usuário adicional (anual = 12x o mensal).</summary>
    public const decimal SeatPriceMonthly = 19.90m;

    /// <summary>Somente o plano Negócio pode contratar assentos adicionais.</summary>
    public static bool IsEligiblePlan(string? effectivePlanCode) =>
        string.Equals(effectivePlanCode, BusinessPlanCode, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Novo teto do recurso com os incrementos comprados. Teto ilimitado/ausente (null)
    /// permanece sem teto; incrementos negativos são tratados como zero.
    /// </summary>
    public static int? AddLimitIncrement(int? baseLimit, int purchasedIncrement) =>
        baseLimit is null ? null : baseLimit + Math.Max(0, purchasedIncrement);

    /// <summary>Mensagem de erro para a quantidade pedida; null quando a compra é válida.</summary>
    public static string? ValidatePurchase(int currentPurchased, int requestedQuantity)
    {
        if (requestedQuantity < 1 || requestedQuantity > MaxAdditionalSeats)
            return $"Contrate de 1 a {MaxAdditionalSeats} usuário(s) adicional(is) por vez.";

        if (currentPurchased >= MaxAdditionalSeats)
            return $"Sua conta já atingiu o máximo de {MaxAdditionalSeats} usuários adicionais.";

        if (currentPurchased + requestedQuantity > MaxAdditionalSeats)
            return $"Com {currentPurchased} já contratado(s), sobra vaga para {MaxAdditionalSeats - currentPurchased} usuário(s) adicional(is).";

        return null;
    }

    /// <summary>Custo mensal total dos assentos informados.</summary>
    public static decimal MonthlyCost(int seats) => Math.Max(0, seats) * SeatPriceMonthly;
}
