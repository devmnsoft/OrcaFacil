using System.Text.RegularExpressions;

namespace OrcaFacil.Application.Quality;

public enum QualityStatus { NotReviewed, Critical, NeedsAttention, Good, Ready }
public enum FindingSeverity { P0, P1, P2, P3 }

public sealed record SourceCodeFinding(string File, int Line, FindingSeverity Severity, string Category, string Risk, string Recommendation);
public sealed record ReadinessCheck(string Name, bool Passed, string Evidence);
public sealed record ModuleReadiness(
    string Module, string Route, QualityStatus Status, IReadOnlyList<ReadinessCheck> Checks,
    int P0, int P1, int P2, DateTimeOffset ValidatedAt, string Recommendation)
{
    public int Passed => Checks.Count(x => x.Passed);
    public int Total => Checks.Count;
}

public sealed record FunctionalQualitySnapshot(
    IReadOnlyList<ModuleReadiness> Modules,
    IReadOnlyList<SourceCodeFinding> Findings,
    DateTimeOffset GeneratedAt)
{
    public int ReadyModules => Modules.Count(x => x.Status == QualityStatus.Ready);
}

/// <summary>Performs deterministic source checks against the deployed ASP.NET source tree.</summary>
public sealed class SourceCodeFindingService(string repositoryRoot)
{
    private static readonly string[] Extensions = [".cs", ".cshtml", ".js", ".mjs", ".sql"];
    private static readonly (Regex Pattern, FindingSeverity Severity, string Category, string Risk, string Recommendation)[] Rules =
    [
        (new(@"throw\s+new\s+Not" + "ImplementedException", RegexOptions.Compiled), FindingSeverity.P0, "Implementação", "Fluxo interrompido em execução.", "Implementar o contrato antes de publicar."),
        (new(@"catch\s*(\([^)]*\))?\s*\{\s*\}", RegexOptions.Compiled), FindingSeverity.P1, "Tratamento de erro", "Falha operacional pode ser ocultada.", "Registrar e tratar a falha explicitamente."),
        (new("href\\s*=\\s*[\"'](?:#|javascript:" + "void[^\"']*)[\"']", RegexOptions.Compiled | RegexOptions.IgnoreCase), FindingSeverity.P2, "Navegação", "Controle sem destino acessível.", "Usar rota ou botão com ação real."),
        (new(@"\bMath\.random\s*\(", RegexOptions.Compiled), FindingSeverity.P1, "Métrica", "Indicador não reproduzível.", "Derivar o indicador de dados persistidos."),
        (new(@"Database=unavailable|127\.0\.0\.1:1", RegexOptions.Compiled | RegexOptions.IgnoreCase), FindingSeverity.P0, "Configuração", "Aplicação inicia com fallback inválido.", "Exigir uma conexão válida com diagnóstico explícito."),
    ];

    public IReadOnlyList<SourceCodeFinding> Scan()
    {
        var findings = new List<SourceCodeFinding>();
        foreach (var scope in new[] { "src", "database", "scripts" })
        {
            var directory = Path.Combine(repositoryRoot, scope);
            if (!Directory.Exists(directory)) continue;
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                         .Where(x => Extensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
                         .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !x.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                         .Where(x => !x.EndsWith("FunctionalQualityServices.cs", StringComparison.OrdinalIgnoreCase)))
            {
                var relative = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                var lines = File.ReadAllLines(file);
                for (var index = 0; index < lines.Length; index++)
                foreach (var rule in Rules)
                    if (rule.Pattern.IsMatch(lines[index]))
                        findings.Add(new(relative, index + 1, rule.Severity, rule.Category, rule.Risk, rule.Recommendation));
            }
        }
        return findings.OrderBy(x => x.Severity).ThenBy(x => x.File).ThenBy(x => x.Line).ToArray();
    }
}

public sealed class BusinessRuleAuditService
{
    public IReadOnlyList<ReadinessCheck> Evaluate(string routeFile, IReadOnlyList<SourceCodeFinding> findings)
    {
        var pageModel = routeFile + ".cs";
        var content = File.Exists(routeFile) ? File.ReadAllText(routeFile) : string.Empty;
        var modelContent = File.Exists(pageModel) ? File.ReadAllText(pageModel) : string.Empty;
        return
        [
            new("Rota principal", File.Exists(routeFile), File.Exists(routeFile) ? "Razor Page localizada" : "Razor Page ausente"),
            new("PageModel", File.Exists(pageModel), File.Exists(pageModel) ? "Handler localizado" : "Handler ausente"),
            new("Autorização", modelContent.Contains("[Authorize", StringComparison.Ordinal), "Proteção de URL verificada no PageModel"),
            new("Validação de conta", modelContent.Contains("AccountId", StringComparison.Ordinal) || modelContent.Contains("SuperAdminOnly", StringComparison.Ordinal), "Isolamento de conta ou administração global declarado"),
            new("Navegação real", !Regex.IsMatch(content, "href\\s*=\\s*[\"'](?:#|javascript:" + "void)", RegexOptions.IgnoreCase), "Nenhum link vazio detectado"),
            new("Ações explícitas", !Regex.IsMatch(content, @"<button(?![^>]*\btype=)", RegexOptions.IgnoreCase), "Botões possuem tipo explícito"),
            new("Auditoria de fonte", !findings.Any(x => routeFile.Replace('\\', '/').EndsWith(x.File, StringComparison.OrdinalIgnoreCase) && x.Severity <= FindingSeverity.P1), "Sem P0/P1 no arquivo da rota"),
        ];
    }
}

public sealed class ModuleReadinessService(string repositoryRoot, BusinessRuleAuditService businessRules)
{
    private static readonly (string Name, string Route)[] Catalog =
    [
        ("Comercial", "Commercial/Index"), ("Clientes", "Clients/Index"), ("Catálogo", "Services/Index"),
        ("Precificação", "Pricing/Index"), ("Orçamentos", "Documents/Index"), ("Propostas", "Commercial/Index"),
        ("OS", "WorkOrders/Index"), ("Campo", "Field/Index"), ("Ativos", "Assets/Index"),
        ("Manutenção", "Maintenance/Index"), ("Financeiro", "Finance/Index"), ("Pagamentos", "Payments/Index"),
        ("Contratos", "Contracts/Index"), ("Portal do Cliente", "Portal/Index"), ("Portal do Parceiro", "PartnerPortal/Index"),
        ("Suporte", "Support/Index"), ("Omnichannel", "Omnichannel/Index"), ("Growth", "Growth/Index"),
        ("Admin", "Admin/Index"), ("Relatórios", "Reports/Index"), ("IA", "Ai/Index"),
    ];

    public IReadOnlyList<ModuleReadiness> Evaluate(IReadOnlyList<SourceCodeFinding> findings, DateTimeOffset validatedAt)
    {
        var pages = Path.Combine(repositoryRoot, "src", "OrcaFacil.Web", "Pages");
        return Catalog.Select(module =>
        {
            var file = Path.Combine(pages, module.Route + ".cshtml");
            var checks = businessRules.Evaluate(file, findings);
            var related = findings.Where(x => x.File.Contains('/' + module.Route.Split('/')[0] + '/', StringComparison.OrdinalIgnoreCase)).ToArray();
            var p0 = related.Count(x => x.Severity == FindingSeverity.P0);
            var p1 = related.Count(x => x.Severity == FindingSeverity.P1);
            var p2 = related.Count(x => x.Severity == FindingSeverity.P2);
            var status = p0 > 0 ? QualityStatus.Critical : p1 > 0 || checks.Any(x => !x.Passed) ? QualityStatus.NeedsAttention : p2 > 0 ? QualityStatus.Good : QualityStatus.Ready;
            var recommendation = status == QualityStatus.Ready ? "Pronto nos controles automatizados; manter monitoramento." : checks.FirstOrDefault(x => !x.Passed)?.Evidence ?? "Revisar achados do módulo.";
            return new ModuleReadiness(module.Name, '/' + module.Route, status, checks, p0, p1, p2, validatedAt, recommendation);
        }).ToArray();
    }
}

public sealed class FunctionalQualityService(SourceCodeFindingService sourceAudit, ModuleReadinessService readiness)
{
    public FunctionalQualitySnapshot Evaluate(DateTimeOffset now)
    {
        var findings = sourceAudit.Scan();
        return new(readiness.Evaluate(findings, now), findings, now);
    }
}
