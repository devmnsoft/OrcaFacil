using OrcaFacil.Application.Abstractions;

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
            "Aplicar a migration QualityGateSchemaDriftV62 e executar novamente."));

        var pagesRoot = Path.Combine(repositoryRoot, "src", "OrcaFacil.Web", "Pages");
        foreach (var route in CriticalRoutes)
        {
            var page = Path.Combine(pagesRoot, route.Replace('/', Path.DirectorySeparatorChar) + ".cshtml");
            var exists = File.Exists(page);
            rules.Add(new($"route.{route.ToLowerInvariant().Replace('/', '.')}", "Rotas", '/' + route.Replace("/Index", string.Empty), exists,
                exists ? "Razor Page e rota física localizadas." : "Razor Page não localizada.",
                $"Restaurar a página crítica {route}."));
        }

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
