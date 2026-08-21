namespace OrcaFacil.Web.Services;

public sealed record NavigationItem(
    string Id, string Label, string Description, string Icon, string Page, string Group,
    string? RequiredPermission, bool IsAdminOnly, bool IsVisible, int Order,
    IReadOnlyList<string> Keywords, bool IsAction = false);

public sealed record NavigationGroup(string Id, string Label, int Order, IReadOnlyList<NavigationItem> Items);

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
        Group("principal", "Principal", 10,
            Item("dashboard", "Dashboard", "Visão geral da operação", "dashboard", "/Dashboard/Index", 10, "painel", "início"),
            Item("search", "Busca global", "Encontre registros autorizados da conta", "search", "/Search/Index", 20, false, "Search.Global", "buscar", "localizar"),
            Item("command-center", "Command Center", "Acesse ações rápidas autorizadas", "plus", "/CommandCenter/Index", 30, false, "CommandCenter.Use", "atalhos", "ações"),
            Item("assistant", "Assistente interno", "Consulte regras, dados permitidos e ajuda", "help", "/Assistant/Index", 40, false, "Assistant.Use", "orientação", "perguntar")),
        Group("comercial", "Comercial", 20,
            Item("routine", "Rotina comercial", "Prioridades e próximos contatos", "calendar", "/CommercialRoutine/Index", 10, "follow-up"),
            Item("new-budget", "Novo orçamento", "Crie uma proposta comercial", "quote", "/Documents/New", 20, true, "documents.create", "proposta", "criar orçamento"),
            Item("budgets", "Orçamentos", "Propostas e documentos comerciais", "quote", "/Documents/Index", 30, false, "documents.read", "propostas"),
            Item("clients", "Clientes", "Carteira e histórico de clientes", "client", "/Clients/Index", 40, false, "clients.read", "contatos"),
            Item("messages", "Templates de mensagem", "Mensagens comerciais reutilizáveis", "share", "/MessageTemplates/Index", 50, "mensagens")),
        Group("operation", "Operação", 30,
            Item("orders", "Ordens de serviço", "Execução e acompanhamento das OS", "work-order", "/WorkOrders/Index", 10, "os", "ordem"),
            Item("schedule", "Agenda", "Compromissos e entregas", "calendar", "/Schedule/Index", 20, "calendário"),
            Item("contracts", "Contratos", "Receitas e serviços recorrentes", "document", "/Contracts/Index", 30, "recorrência")),
        Group("finance", "Financeiro", 40,
            Item("payments", "Pagamentos", "Recebimentos e conciliação", "payment", "/Payments/Index", 10, "cobranças"),
            Item("receipts", "Recibos", "Comprovantes emitidos", "receipt", "/Receipts/Index", 20, false, "receipts.read", "comprovantes"),
            Item("plan", "Meu plano", "Assinatura e limites da conta", "plan", "/Subscription/Index", 30, "assinatura")),
        Group("intelligence", "Inteligência", 50,
            Item("reports", "Relatórios", "Indicadores comerciais e financeiros", "chart", "/Reports/Index", 10, "indicadores", "dashboard executivo"),
            Item("analytics", "BI Executivo", "KPIs e comparativos reais", "chart", "/Analytics/Executive", 20, false, "Analytics.Executive", "analytics", "indicadores"),
            Item("forecast", "Forecast", "Projeção determinística e explicável", "chart", "/Analytics/Forecast", 30, false, "Analytics.Forecast", "previsão"),
            Item("data-quality", "Qualidade dos Dados", "Inconsistências com ações de correção", "notification", "/Analytics/DataQuality", 40, false, "DataQuality.View", "qualidade"),
            Item("account-health", "Saúde da Conta", "Score operacional explicável", "chart", "/Analytics/AccountHealth", 50, false, "AccountHealth.View", "saúde"),
            Item("alerts", "Alertas", "Pendências que exigem atenção", "notification", "/Alerts/Index", 60, "notificações")),
        Group("administration", "Administração", 60,
            Item("services", "Serviços", "Catálogo de serviços e preços", "service", "/Services/Index", 10, false, "services.read", "catálogo"),
            Item("templates", "Templates", "Modelos de orçamento", "quote-ready", "/Templates/Index", 20, false, "templates.read", "modelos"),
            Item("import", "Importação", "Importe dados para a conta", "upload", "/Import/Index", 30, "arquivos"),
            Item("settings", "Configurações", "Empresa, documentos e segurança", "settings", "/Settings/Index", 40, "preferências")),
        Group("account", "Conta", 70,
            Item("profile", "Dados do emitente", "Identidade da empresa", "account", "/Profile/Index", 10, "perfil"),
            Item("notifications", "Notificações", "Central de atividades", "notification", "/Notifications/Index", 20, "avisos"),
            Item("help", "Base de conhecimento", "Artigos publicados e ajuda contextual", "help", "/Help/Index", 30, false, "KnowledgeBase.View", "ajuda", "artigos"),
            Item("productivity", "Produtividade", "Prioridades reais da rotina", "chart", "/Productivity/Index", 40, false, "Productivity.View", "pendências", "hoje"),
            Item("support", "Suporte", "Ajuda e atendimento", "help", "/Support/Index", 50, "ajuda"))
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
    private static NavigationGroup Group(string id, string label, int order, params NavigationItem[] items) =>
        new(id, label, order, items.Select(x => x with { Group = id }).ToArray());
    private static NavigationItem Item(string id, string label, string description, string icon, string page, int order, params string[] keywords) =>
        new(id, label, description, icon, page, "", null, false, true, order, keywords);
    private static NavigationItem Item(string id, string label, string description, string icon, string page, int order, bool action, string permission, params string[] keywords) =>
        new(id, label, description, icon, page, "", permission, false, true, order, keywords, action);
}
