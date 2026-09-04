using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.GoLive;

public static class GoLiveChecklistCatalog
{
    public static readonly IReadOnlyList<(string Code, string Title, bool Critical, bool Automatic)> Items =
    [
        ("database", "Banco conectado", true, true), ("schema", "Schema atualizado", true, true),
        ("admin", "Usuário administrador criado", true, true), ("account", "Conta ativa", true, true),
        ("plan", "Plano ativo ou liberação manual válida", true, true), ("issuer", "Perfil emissor preenchido", true, true),
        ("client", "Cliente inicial cadastrado", false, true), ("service", "Serviço inicial cadastrado", false, true),
        ("template", "Template inicial ou de sistema disponível", true, true), ("budget", "Primeiro orçamento criado", false, true),
        ("dashboard", "Dashboard carregando", true, true), ("commercial", "Rotina comercial carregando", true, true),
        ("documents-new", "Documents/New carregando", true, true), ("email", "E-mail configurado ou pendência registrada", false, false),
        ("backup", "Backup configurado", true, false), ("logs", "Logs configurados", true, false),
        ("health", "SystemHealth OK", true, true), ("quality", "QualityGate OK", true, true),
        ("permissions", "Permissões revisadas", true, false), ("portals", "Portais revisados", false, false),
        ("mobile", "Mobile revisado", false, false), ("console", "Console sem erro", true, false)
    ];
}

public sealed class GoLiveChecklistService
{
    public GoLiveStatus ResolveStatus(IEnumerable<GoLiveChecklistItem> items, GoLiveStatus requested)
    {
        var all = items.ToArray();
        if (all.Length == 0) return GoLiveStatus.NotStarted;
        if (all.Any(x => x.IsCritical && !x.IsCompleted) && requested is GoLiveStatus.ReadyForPilot or GoLiveStatus.ReadyForProduction or GoLiveStatus.Live)
            return GoLiveStatus.Blocked;
        return requested;
    }

    public void CompleteManual(GoLiveChecklistItem item, Guid accountId, Guid userId, string responsible, string observation, bool confirmed)
    {
        if (item.AccountId != accountId) throw new UnauthorizedAccessException("O item pertence a outra conta.");
        if (item.IsAutomatic) throw new InvalidOperationException("Itens automáticos são avaliados por evidência do sistema.");
        if (!confirmed || userId == Guid.Empty || string.IsNullOrWhiteSpace(responsible) || string.IsNullOrWhiteSpace(observation))
            throw new InvalidOperationException("Confirme a verificação e informe responsável e observação.");
        item.IsCompleted = true; item.CompletedByUserId = userId; item.CompletedAt = DateTime.UtcNow;
        item.ResponsibleName = responsible.Trim(); item.Observation = observation.Trim(); item.Touch();
    }
}

public sealed record TrainingLesson(string Code, string Title, string Description, string Route, string Track);
public sealed class TrainingGuideService
{
    public IReadOnlyList<TrainingLesson> GetLessons(bool isTechnicalAdmin) =>
    new TrainingLesson[]
    {
        new("first-steps", "Primeiros passos", "Complete o perfil e conheça as próximas ações.", "/Onboarding", "Operação"),
        new("clients", "Cadastrar cliente", "Crie o cliente usado no orçamento.", "/Clients/Create", "Comercial"),
        new("services", "Cadastrar serviço", "Padronize unidade e preço.", "/Services/Create", "Comercial"),
        new("budget", "Criar primeiro orçamento", "Monte e revise uma proposta real.", "/Documents/CreateBudget", "Documentos"),
        new("routine", "Acompanhar rotina comercial", "Registre retornos e próximos passos.", "/CommercialRoutine", "Comercial"),
        new("dashboard", "Entender Dashboard", "Leia indicadores reais da conta.", "/Dashboard", "Operação"),
        new("security", "Boas práticas de segurança", "Proteja sessão e acessos.", "/Settings/Security", "Admin")
    }.Where(x => isTechnicalAdmin || x.Code != "security").ToArray();
}

public sealed class TrainingProgressService
{
    public void Complete(TrainingProgress progress, Guid accountId, Guid userId, bool confirmed)
    {
        if (progress.AccountId != accountId || progress.UserId != userId) throw new UnauthorizedAccessException("Progresso de outra conta ou usuário.");
        if (!confirmed) throw new InvalidOperationException("A conclusão exige confirmação do usuário.");
        progress.UserConfirmed = true; progress.CompletedAt = DateTime.UtcNow; progress.Touch();
    }
}

public sealed class RouteErrorFingerprintService
{
    public string Create(string exceptionType, string route) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{exceptionType}|{route}")))[..16];
    public string Sanitize(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Falha não detalhada.";
        var blocked = new[] { "password", "token", "secret", "connectionstring", "authorization" };
        return blocked.Any(x => message.Contains(x, StringComparison.OrdinalIgnoreCase)) ? "Detalhes protegidos. Consulte o correlation id." : message[..Math.Min(message.Length, 500)];
    }
}

public sealed record ProductionCheck(string Code, string Label, bool Passed, bool Blocking, string Recommendation);
public sealed class ProductionReadinessService
{
    public bool IsReady(IEnumerable<ProductionCheck> checks) => checks.Any() && checks.All(x => !x.Blocking || x.Passed);
    public void ValidateConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("Database=unavailable", StringComparison.OrdinalIgnoreCase) || value.Contains("127.0.0.1:1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A conexão de produção não está configurada com um destino operacional.");
    }
}
