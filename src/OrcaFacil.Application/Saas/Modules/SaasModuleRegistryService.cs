namespace OrcaFacil.Application.Saas.Modules;

public sealed record SaasModuleDefinition(string Code, string DisplayName, string Category, string IconKey,
    string MenuGroup, string RoutePrefix, string RequiredPermissionCode, decimal MonthlyPrice, decimal AnnualPrice,
    int DisplayOrder, bool IsPublic = true, bool IsActive = true);

public sealed class SaasModuleRegistryService
{
    public static readonly IReadOnlyList<SaasModuleDefinition> InitialModules =
    [
        new("CORE", "Core", "Base", "home", "Principal", "/Dashboard", "Dashboard.View", 0, 0, 10, false),
        new("CLIENTS", "Clientes", "Comercial", "users", "Comercial", "/Clients", "Clients.View", 39, 390, 20),
        new("DOCUMENTS", "Orçamentos e Propostas", "Comercial", "file-text", "Comercial", "/Documents", "Documents.View", 69, 690, 30),
        new("COMMERCIAL_ROUTINE", "Rotina Comercial", "Comercial", "trending-up", "Comercial", "/CommercialRoutine", "CommercialRoutine.View", 49, 490, 40),
        new("WORK_ORDERS", "Ordens de Serviço", "Operações", "tool", "Operações", "/WorkOrders", "WorkOrders.View", 79, 790, 50),
        new("SCHEDULE", "Agenda e Campo", "Operações", "calendar", "Operações", "/Schedule", "Schedule.View", 49, 490, 60),
        new("FINANCIAL", "Financeiro", "Gestão", "dollar-sign", "Gestão", "/CashFlow", "Finance.View", 89, 890, 70),
        new("FISCAL", "Fiscal", "Gestão", "clipboard", "Gestão", "/Fiscal", "Fiscal.View", 99, 990, 80, true, false),
        new("PROJECTS", "Projetos", "Gestão", "briefcase", "Gestão", "/Projects", "Projects.View", 69, 690, 90, true, false),
        new("CUSTOMER_SUCCESS", "Customer Success", "Relacionamento", "heart", "Relacionamento", "/CustomerSuccess", "CustomerSuccess.View", 59, 590, 100, true, false),
        new("BI", "BI Executivo", "Inteligência", "bar-chart", "Inteligência", "/Bi", "BI.View", 99, 990, 110, true, false),
        new("AUTOMATION", "Automação", "Inteligência", "zap", "Inteligência", "/Automation", "Automation.View", 79, 790, 120, true, false),
        new("DATA_GOVERNANCE", "Governança de Dados", "Governança", "shield", "Governança", "/DataGovernance", "DataQuality.View", 89, 890, 130, true, false),
        new("CLIENT_PORTAL", "Portal do Cliente", "Portais", "external-link", "Portais", "/Portal", "Portal.View", 49, 490, 140, true, false),
        new("PARTNER_PORTAL", "Portal do Parceiro", "Portais", "link", "Portais", "/Partners", "Partners.View", 49, 490, 150, true, false),
        new("SUPPORT", "Suporte", "Sucesso", "help-circle", "Sucesso", "/Support", "Support.View", 29, 290, 160),
        new("TRAINING", "Treinamento", "Sucesso", "book-open", "Sucesso", "/Training", "Training.View", 19, 190, 170),
        new("ACCOUNT_ADMIN", "Administração", "Administração", "settings", "Administração", "/AccountAdmin", "Account.Users.View", 0, 0, 180, false),
        new("QUALITY_GATE", "Quality Gate", "Governança", "check-circle", "Governança", "/Admin/QualityGate", "QualityGate.View", 0, 0, 190, false)
    ];

    public IReadOnlyList<SaasModuleDefinition> GetAll() => InitialModules;
    public SaasModuleDefinition? Find(string code) => InitialModules.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    public SaasModuleDefinition? FindByPath(string path)
    {
        var alias = path.StartsWith("/CommercialPipeline", StringComparison.OrdinalIgnoreCase) ? "COMMERCIAL_ROUTINE" :
            path.StartsWith("/Payments", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/Receivables", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/BankAccounts", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/Reports/Financial", StringComparison.OrdinalIgnoreCase) ? "FINANCIAL" :
            path.StartsWith("/Templates", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/Services", StringComparison.OrdinalIgnoreCase) ? "DOCUMENTS" : null;
        if (alias is not null) return InitialModules.FirstOrDefault(x => x.Code == alias && x.IsActive);
        return InitialModules.Where(x => x.IsActive && path.StartsWith(x.RoutePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.RoutePrefix.Length).FirstOrDefault();
    }
}
