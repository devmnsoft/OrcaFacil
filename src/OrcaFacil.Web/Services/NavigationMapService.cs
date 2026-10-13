namespace OrcaFacil.Web.Services;

public sealed record NavigationItem(
    string Id, string Label, string Description, string Icon, string Page, string Group,
    string? RequiredPermission, bool IsAdminOnly, bool IsVisible, int Order,
    IReadOnlyList<string> Keywords, bool IsAction = false);

public sealed record NavigationGroup(string Id, string Label, int Order, IReadOnlyList<NavigationItem> Items, bool Primary = true);

public interface INavigationMapService
{
    IReadOnlyList<NavigationGroup> GetGroups(IReadOnlyCollection<string> permissions, bool isAdmin = false);
    IReadOnlyList<NavigationItem> Search(string query, IReadOnlyCollection<string> permissions, bool isAdmin = false, int limit = 8);
}

/// <summary>Single source of truth for authenticated links. Every Page below has a corresponding Razor Page.</summary>
public sealed class NavigationMapService : INavigationMapService
{
    private static readonly NavigationGroup[] Groups =
    [
        Group("overview", "Visão geral", 10, true,
            Item("dashboard", "Visão geral", "Prioridades do dia na conta atual", "dashboard", "/Dashboard/Index", 10, "painel", "início")),
        Group("clients", "Clientes", 20, true,
            Item("clients", "Clientes", "Carteira e histórico de clientes", "client", "/Clients/Index", 10, false, "Clients.View", "contatos")),
        Group("catalog", "Catálogo", 30, true,
            Item("services", "Serviços", "Itens, unidades e preços do catálogo", "service", "/Services/Index", 10, false, "Documents.View", "catálogo"),
            Item("templates", "Modelos", "Modelos de orçamento reutilizáveis", "quote-ready", "/Templates/Index", 20, false, "Documents.View", "modelos")),
        Group("quotes", "Orçamentos", 40, true,
            Item("budgets", "Orçamentos", "Propostas, revisões e decisões", "quote", "/Documents/Index", 10, false, "Documents.View", "propostas"),
            Item("new-budget", "Novo orçamento", "Crie uma proposta comercial", "quote", "/Documents/New", 20, true, "documents.create", "proposta", "criar orçamento"),
            Item("budget-assistant", "Sugestões do orçamento", "Sugestões editáveis, aplicadas só depois da confirmação", "help", "/Documents/BudgetAssistant", 30, false, "documents.create", "ia", "sugestão")),
        Group("operations", "Ordens de serviço", 50, true,
            Item("orders", "Ordens de serviço", "Escopo contratado, agenda e andamento", "work-order", "/WorkOrders/Index", 10, false, "WorkOrders.View", "os", "ordem"),
            Item("schedule", "Agenda", "Serviços com data marcada", "calendar", "/Schedule/Index", 20, false, "Schedule.View", "calendário")),
        Group("finance", "Financeiro", 60, true,
            Item("payments", "Recebimentos", "Valores recebidos, parciais e estornos", "payment", "/Payments/Index", 10, false, "Finance.View", "cobranças", "pagamentos"),
            Item("receivables", "Contas a receber", "Vencimentos e saldo em aberto", "payment", "/Receivables/Index", 20, false, "Finance.View", "vencidos"),
            Item("receipts", "Recibos", "Comprovantes de pagamentos já registrados", "receipt", "/Receipts/Index", 30, false, "Finance.View", "comprovantes")),
        Group("settings", "Configurações", 70, true,
            Item("profile", "Dados do emitente", "Identidade usada nas propostas", "account", "/Profile/Index", 10, "perfil"),
            Item("settings", "Configurações", "Empresa, documentos e segurança", "settings", "/Settings/Index", 20, "preferências"),
            Item("plan", "Assinatura", "Plano e limites da conta", "plan", "/Subscription/Index", 30, "assinatura", "plano")),
        Group("more-commercial", "Outras ferramentas", 80, false,
            Item("routine", "Rotina comercial", "Prioridades e próximos contatos", "calendar", "/CommercialRoutine/Index", 10, "follow-up"),
            Item("messages", "Mensagens", "Textos comerciais reutilizáveis", "share", "/MessageTemplates/Index", 20, "mensagens"),
            Item("contracts", "Contratos", "Receitas e serviços recorrentes", "document", "/Contracts/Index", 30, "recorrência"),
            Item("search", "Busca", "Registros autorizados da conta", "search", "/Search/Index", 40, false, "Search.Global", "buscar", "localizar"),
            Item("reports", "Relatórios", "Indicadores comerciais e financeiros", "chart", "/Reports/Index", 50, "indicadores"),
            Item("alerts", "Alertas", "Pendências que exigem atenção", "notification", "/Alerts/Index", 60, "notificações"),
            Item("notifications", "Notificações", "Atividades da conta", "notification", "/Notifications/Index", 70, "avisos"),
            Item("support", "Suporte", "Ajuda e atendimento", "help", "/Support/Index", 80, "ajuda"),
            Item("help", "Como usar", "Artigos e ajuda contextual", "help", "/Help/Index", 90, false, "KnowledgeBase.View", "ajuda", "artigos"),
            Item("import", "Importação", "Importe dados para a conta", "upload", "/Import/Index", 100, "arquivos"),
            Item("assistant", "Assistente", "Consulta regras e dados permitidos", "help", "/Assistant/Index", 110, false, "Assistant.Use", "orientação"),
            Item("command-center", "Ações rápidas", "Atalhos autorizados", "plus", "/CommandCenter/Index", 120, false, "CommandCenter.Use", "atalhos"),
            Item("analytics", "Indicadores executivos", "Comparativos da conta", "chart", "/Analytics/Executive", 130, false, "Analytics.Executive", "analytics"),
            Item("forecast", "Previsão", "Projeção explicável da conta", "chart", "/Analytics/Forecast", 140, false, "Analytics.Forecast", "previsão"),
            Item("data-quality", "Qualidade dos dados", "Inconsistências com correção", "notification", "/Analytics/DataQuality", 150, false, "DataQuality.View", "qualidade"),
            Item("account-health", "Saúde da conta", "Leitura operacional da conta", "chart", "/Analytics/AccountHealth", 160, false, "AccountHealth.View", "saúde"),
            Item("productivity", "Produtividade", "Prioridades da rotina", "chart", "/Productivity/Index", 170, false, "Productivity.View", "pendências"),
            Item("quality", "Qualidade funcional", "Auditoria dos módulos existentes", "health", "/Admin/Quality/Index", 180, false, "Quality.View", "auditoria"),
            Item("journey-refinement", "Revisão de jornadas", "Pendências derivadas de verificações reais", "health", "/Admin/JourneyRefinement/Index", 190, false, "JourneyRefinement.View", "jornadas"))
    ];

    public IReadOnlyList<NavigationGroup> GetGroups(IReadOnlyCollection<string> permissions, bool isAdmin = false) => Groups
        .OrderBy(x => x.Order).Select(group => group with { Items = group.Items.Where(x => CanSee(x, permissions, isAdmin)).OrderBy(x => x.Order).ToArray() })
        .Where(x => x.Items.Count > 0).ToArray();

    public IReadOnlyList<NavigationItem> Search(string query, IReadOnlyCollection<string> permissions, bool isAdmin = false, int limit = 8)
    {
        var term = query.Trim();
        if (term.Length < 2) return [];
        return GetGroups(permissions, isAdmin).SelectMany(x => x.Items)
            .Where(x => x.Label.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(term, StringComparison.OrdinalIgnoreCase) || x.Keywords.Any(k => k.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(Math.Clamp(limit, 1, 12)).ToArray();
    }

    private static bool CanSee(NavigationItem item, IReadOnlyCollection<string> permissions, bool isAdmin) =>
        item.IsVisible && (!item.IsAdminOnly || isAdmin) && (item.RequiredPermission is null || permissions.Contains(item.RequiredPermission));
    private static NavigationGroup Group(string id, string label, int order, bool primary, params NavigationItem[] items) =>
        new(id, label, order, items.Select(x => x with { Group = id }).ToArray(), primary);
    private static NavigationItem Item(string id, string label, string description, string icon, string page, int order, params string[] keywords) =>
        new(id, label, description, icon, page, "", null, false, true, order, keywords);
    private static NavigationItem Item(string id, string label, string description, string icon, string page, int order, bool action, string permission, params string[] keywords) =>
        new(id, label, description, icon, page, "", permission, false, true, order, keywords, action);
}
