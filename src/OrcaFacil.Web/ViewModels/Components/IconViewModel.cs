namespace OrcaFacil.Web.ViewModels.Components;

public sealed record IconViewModel(
    string Name,
    string Size = "md",
    string? Label = null,
    bool Decorative = true,
    string? Tone = null,
    string? CssClass = null);

public static class IconRegistry
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "dashboard", "search", "command", "notification", "help", "account", "switch-account", "profile", "security", "logout", "menu", "collapse", "expand", "chevron-down", "chevron-right", "plus", "close",
        "quote", "quote-draft", "quote-ready", "quote-sent", "quote-viewed", "negotiation", "approved", "rejected", "expired", "pipeline", "follow-up", "version", "attachment", "share", "copy-link", "email", "whatsapp", "preview", "pdf",
        "client", "person", "company", "address", "phone", "email-address", "tag", "favorite", "service", "category", "unit", "price", "cost", "margin", "history",
        "work-order", "checklist", "calendar", "clock", "start", "pause", "complete", "cancel", "responsible", "route", "evidence", "camera", "file",
        "payment", "pix", "cash", "card", "transfer", "boleto", "receipt", "partial-payment", "balance", "refund",
        "plan", "free-plan", "professional-plan", "business-plan", "premium", "limit", "usage", "paused", "restored", "protected-data",
        "operation", "account-360", "users", "billing", "revenue", "database", "migration", "email-queue", "worker", "health", "error", "audit", "settings",
        "success", "warning", "information", "blocked", "unavailable", "offline", "loading", "empty", "placeholder"
    };

    public static string Resolve(string? name) => !string.IsNullOrWhiteSpace(name) && Names.Contains(name) ? name.ToLowerInvariant() : "placeholder";
}
