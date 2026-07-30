namespace OrcaFacil.Application.Plans;

/// <summary>Canonical policy for lossless plan transitions.</summary>
public static class PlanDataPreservationPolicy
{
    public const string Principle = "Planos controlam acesso e capacidade. Eles não apagam os dados.";
    public static readonly IReadOnlySet<string> PreservedData = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "clients", "services", "documents", "pdfs", "receipts", "revisions", "public-links",
        "decisions", "follow-ups", "work-orders", "calendar", "members", "invitations", "audit",
        "settings", "branding", "logo", "templates", "history", "notifications"
    };

    public static PlanCapacityDecision Evaluate(int currentUsage, int? availableLimit)
    {
        if (!availableLimit.HasValue) return new(true, currentUsage, null, 0, null);
        var excess = Math.Max(0, currentUsage - availableLimit.Value);
        return new(excess == 0, currentUsage, availableLimit, excess,
            excess == 0 ? null : $"Seus {currentUsage} registros continuam salvos. Novos cadastros estão pausados enquanto o uso permanecer acima do limite disponível.");
    }
}
public sealed record PlanCapacityDecision(bool CanCreate,int PreservedCount,int? AvailableLimit,int AboveLimitCount,string? Message);
