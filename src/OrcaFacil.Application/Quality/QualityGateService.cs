using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Saas.Modules;

namespace OrcaFacil.Application.Quality;

public sealed record QualityGateRule(string Code, string Category, string Description, bool Passed, string Evidence, string Recommendation);

public sealed record QualityGateSnapshot(
    IReadOnlyList<QualityGateRule> Rules,
    DateTimeOffset ExecutedAt,
    string Responsible)
{
    public int Passed => Rules.Count(rule => rule.Passed);
    public int Failed => Rules.Count - Passed;
    public int Score => Rules.Count == 0 ? 0 : (int)Math.Round(Passed * 100m / Rules.Count);
    public bool IsApproved => Failed == 0;
    public string NextAction => Rules.FirstOrDefault(rule => !rule.Passed)?.Recommendation
        ?? "Manter o gate no pipeline e acompanhar a próxima execução.";
}

/// <summary>
/// Aggregates live database diagnostics and deterministic source checks. It never
/// invents scores: every point displayed by the UI corresponds to a rule below.
/// </summary>
public sealed class QualityGateService(
    IDatabaseSchemaContractService schema,
    FunctionalQualityService sourceQuality,
    SaasModuleRegistryService moduleRegistry,
    IClock clock)
{
    private static readonly string[] CriticalRoutes =
    [
        "Index", "Auth/Login", "Auth/Register", "Auth/ForgotPassword", "Onboarding/Index",
        "Dashboard/Index", "Clients/Index", "Documents/Index", "Documents/New",
        "CommercialRoutine/Index", "Diagnostico", "Admin/Index", "Portal/Index", "PartnerPortal/Index"
    ];

    public async Task<QualityGateSnapshot> EvaluateAsync(string repositoryRoot, string responsible, CancellationToken ct = default)
    {
        var rules = new List<QualityGateRule>();
        var schemaResult = await schema.CheckRegistrationContractAsync(ct);
        rules.Add(new("schema.critical", "Schema", "Tabelas, colunas, índices e migrations críticos",
            schemaResult.IsValid,
            schemaResult.IsValid ? "Contrato do banco aprovado." : $"{schemaResult.Issues.Count} divergência(s) encontrada(s).",
            "Aplicar a migration SaasEnterpriseMultiTenantModulesV66 e executar novamente."));

        var pagesRoot = Path.Combine(repositoryRoot, "src", "OrcaFacil.Web", "Pages");
        foreach (var route in CriticalRoutes)
        {
            var page = Path.Combine(pagesRoot, route.Replace('/', Path.DirectorySeparatorChar) + ".cshtml");
            var exists = File.Exists(page);
            rules.Add(new($"route.{route.ToLowerInvariant().Replace('/', '.')}", "Rotas", '/' + route.Replace("/Index", string.Empty), exists,
                exists ? "Razor Page e rota física localizadas." : "Razor Page não localizada.",
                $"Restaurar a página crítica {route}."));
        }

        var activeModules = moduleRegistry.GetAll().Where(module => module.IsActive).ToArray();
        foreach (var module in activeModules)
        {
            var relative = module.RoutePrefix.Trim('/').Replace('/', Path.DirectorySeparatorChar);
            var routeExists = File.Exists(Path.Combine(pagesRoot, relative, "Index.cshtml")) ||
                              File.Exists(Path.Combine(pagesRoot, relative + ".cshtml"));
            rules.Add(new($"module.{module.Code.ToLowerInvariant()}.route", "Módulos", $"Rota real do módulo {module.DisplayName}", routeExists,
                routeExists ? module.RoutePrefix : "Rota física ausente.", $"Implementar a Razor Page de {module.DisplayName} antes de ativar o módulo."));
            var validCommercialDefinition = !string.IsNullOrWhiteSpace(module.RequiredPermissionCode) &&
                                            module.MonthlyPrice >= 0 && module.AnnualPrice >= 0;
            rules.Add(new($"module.{module.Code.ToLowerInvariant()}.contract", "Módulos", $"Preço e permissão de {module.DisplayName}", validCommercialDefinition,
                validCommercialDefinition ? $"{module.RequiredPermissionCode}; mensal {module.MonthlyPrice:C}." : "Contrato comercial incompleto.",
                "Configurar preço não negativo e permissão explícita no catálogo."));
        }

        var menuFile = Path.Combine(repositoryRoot, "src", "OrcaFacil.Web", "Services", "ModuleMenuComposer.cs");
        var menuText = File.Exists(menuFile) ? File.ReadAllText(menuFile) : string.Empty;
        var activeTenantModules = activeModules.Where(module => module.Code != "QUALITY_GATE").ToArray();
        var menuComplete = activeTenantModules.All(module => menuText.Contains($"\"{module.Code}\"", StringComparison.Ordinal));
        rules.Add(new("modules.menu", "Módulos", "Todo módulo ativo possui entrada de menu funcional", menuComplete,
            menuComplete ? $"{activeTenantModules.Length} módulos ativos cobertos." : "Há módulo ativo sem composição de menu.",
            "Adicionar a entrada real ao compositor ou manter o módulo inativo."));

        var middlewareFile = Path.Combine(repositoryRoot, "src", "OrcaFacil.Web", "Middleware", "ModuleAccessMiddleware.cs");
        var middlewareText = File.Exists(middlewareFile) ? File.ReadAllText(middlewareFile) : string.Empty;
        var backendGuard = middlewareText.Contains("HasPermissionAsync", StringComparison.Ordinal) &&
                           middlewareText.Contains("access.CheckAsync", StringComparison.Ordinal) &&
                           middlewareText.Contains("Status403Forbidden", StringComparison.Ordinal);
        rules.Add(new("modules.backend-guard", "Segurança", "Permissão e entitlement são validados no backend", backendGuard,
            backendGuard ? "Middleware aplica permissão, assinatura e entitlement." : "Guard de backend incompleto.",
            "Restaurar o guard de módulos antes da autorização de rotas."));

        var source = sourceQuality.Evaluate(clock.UtcNow);
        var blockers = source.Findings.Count(finding => finding.Severity <= FindingSeverity.P1);
        rules.Add(new("source.blockers", "Código", "Sem achados P0/P1 na auditoria estática", blockers == 0,
            $"{blockers} bloqueador(es) em código real.", "Corrigir o primeiro achado P0/P1 listado no System Health."));

        var layout = Path.Combine(pagesRoot, "Shared", "_Layout.cshtml");
        var layoutText = File.Exists(layout) ? File.ReadAllText(layout) : string.Empty;
        rules.Add(new("ui.feedback", "Interface", "Toast e confirmação acessíveis no layout",
            layoutText.Contains("_ToastHost", StringComparison.Ordinal) && layoutText.Contains("_ConfirmDialog", StringComparison.Ordinal),
            "Hosts globais verificados no layout.", "Adicionar os hosts globais de feedback ao layout."));

        return new(rules, clock.UtcNow, string.IsNullOrWhiteSpace(responsible) ? "Execução automatizada" : responsible);
    }
}
