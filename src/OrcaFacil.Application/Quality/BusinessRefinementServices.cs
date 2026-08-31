using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Quality;

public enum BusinessEntityKind { Proposal, WorkOrder, Invoice, Payment, Contract, Asset, Task, FiscalDocument }
public sealed record BusinessTransition(string From, string To, bool RequiresPermission = false, bool RequiresReason = false);

/// <summary>Single, deterministic catalog for user-facing labels and lifecycle transitions.</summary>
public sealed class BusinessStatusCatalogService
{
    private static readonly IReadOnlyDictionary<BusinessEntityKind, IReadOnlyList<BusinessTransition>> Transitions =
        new Dictionary<BusinessEntityKind, IReadOnlyList<BusinessTransition>>
        {
            [BusinessEntityKind.Proposal] = [new("Draft", "Ready"), new("Draft", "Sent"), new("Ready", "Sent"), new("Sent", "Viewed"), new("Sent", "InNegotiation"), new("Sent", "Approved", true), new("Sent", "Rejected"), new("Sent", "Expired"), new("Viewed", "InNegotiation"), new("Viewed", "Approved", true), new("InNegotiation", "Approved", true), new("InNegotiation", "Rejected")],
            [BusinessEntityKind.WorkOrder] = [new("Planned", "Scheduled"), new("Scheduled", "InProgress"), new("InProgress", "Completed", true), new("InProgress", "PendingReview"), new("InProgress", "Paused"), new("PendingReview", "Completed", true), new("Paused", "InProgress")],
            [BusinessEntityKind.Invoice] = [new("Open", "PendingPayment"), new("Open", "Overdue"), new("Open", "Canceled", true, true), new("PendingPayment", "Paid", true), new("PendingPayment", "Overdue"), new("Overdue", "Paid", true), new("Overdue", "Canceled", true, true)],
            [BusinessEntityKind.Payment] = [new("Pending", "Confirmed", true), new("Pending", "Rejected"), new("Confirmed", "Refunded", true, true)],
            [BusinessEntityKind.Contract] = [new("Draft", "Active", true), new("Active", "PendingRenewal"), new("PendingRenewal", "Renewed", true), new("Active", "Terminated", true, true)],
            [BusinessEntityKind.Asset] = [new("Active", "UnderMaintenance"), new("UnderMaintenance", "Active"), new("Active", "Discarded", true, true), new("Discarded", "Active", true, true)],
            [BusinessEntityKind.Task] = [new("Todo", "InProgress"), new("InProgress", "Done"), new("Done", "InProgress", true, true)],
            [BusinessEntityKind.FiscalDocument] = [new("Draft", "Validated"), new("Validated", "Issued", true), new("Issued", "Canceled", true, true)]
        };

    public IReadOnlyList<BusinessTransition> GetTransitions(BusinessEntityKind entity) => Transitions[entity];
    public string GetLabel(BusinessEntityKind entity, string status) => (entity, status) switch
    {
        (BusinessEntityKind.WorkOrder, "PendingReview") => "Aguardando revisão",
        (BusinessEntityKind.Invoice, "PendingPayment") => "Aguardando pagamento",
        (BusinessEntityKind.Asset, "UnderMaintenance") => "Em manutenção",
        (BusinessEntityKind.Task, "InProgress") => "Em andamento",
        _ => status
    };
}

public sealed class BusinessTransitionRuleService(BusinessStatusCatalogService catalog)
{
    public BusinessTransition EnsureAllowed(BusinessEntityKind entity, string current, string next, bool hasCriticalPermission, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(current);
        ArgumentException.ThrowIfNullOrWhiteSpace(next);
        var transition = catalog.GetTransitions(entity).FirstOrDefault(x => x.From == current && x.To == next)
            ?? throw new InvalidOperationException($"A transição de {current} para {next} não é permitida para {entity}.");
        if (transition.RequiresPermission && !hasCriticalPermission)
            throw new UnauthorizedAccessException("Esta transição crítica exige permissão específica.");
        if (transition.RequiresReason && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Informe o motivo da transição crítica.", nameof(reason));
        return transition;
    }
}

public interface IBusinessTransitionAudit
{
    Task RecordAsync(Guid accountId, BusinessEntityKind entity, Guid entityId, string from, string to, Guid actorId, string? reason, CancellationToken cancellationToken);
}

public sealed class EntityLifecycleService(BusinessTransitionRuleService rules, IBusinessTransitionAudit audit)
{
    public async Task TransitionAsync(Guid accountId, BusinessEntityKind entity, Guid entityId, string current, string next,
        Guid actorId, bool hasCriticalPermission, string? reason, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || entityId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Conta, entidade e usuário são obrigatórios.");
        var transition = rules.EnsureAllowed(entity, current, next, hasCriticalPermission, reason);
        if (transition.RequiresPermission)
            await audit.RecordAsync(accountId, entity, entityId, current, next, actorId, reason?.Trim(), cancellationToken);
    }
}

public static class FinancialRoundingPolicy
{
    public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public static class MoneyCalculator
{
    public static decimal ApplyPercentageDiscount(decimal subtotal, decimal percentage)
    {
        EnsureNonNegative(subtotal, nameof(subtotal));
        if (percentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percentage));
        return FinancialRoundingPolicy.Round(subtotal * (1 - percentage / 100m));
    }

    public static decimal ApplyFixedDiscount(decimal subtotal, decimal discount)
    {
        EnsureNonNegative(subtotal, nameof(subtotal));
        EnsureNonNegative(discount, nameof(discount));
        if (discount > subtotal) throw new ArgumentException("O desconto não pode tornar o total negativo.", nameof(discount));
        return FinancialRoundingPolicy.Round(subtotal - discount);
    }

    public static decimal NetAfterRetention(decimal gross, decimal retention)
    {
        EnsureNonNegative(gross, nameof(gross));
        EnsureNonNegative(retention, nameof(retention));
        if (retention > gross) throw new ArgumentException("A retenção não pode tornar o líquido negativo.", nameof(retention));
        return FinancialRoundingPolicy.Round(gross - retention);
    }

    private static void EnsureNonNegative(decimal value, string name) { if (value < 0) throw new ArgumentOutOfRangeException(name); }
}

public sealed class DueDatePolicyService
{
    public void Validate(DateOnly issuedOn, DateOnly dueOn)
    {
        if (dueOn < issuedOn) throw new ArgumentException("O vencimento não pode ser anterior à emissão.", nameof(dueOn));
    }
}

public sealed class PortalIsolationGuardService
{
    public void EnsureClientAccess(Guid accountId, Guid resourceAccountId, Guid clientId, Guid resourceClientId)
    {
        if (accountId == Guid.Empty || clientId == Guid.Empty || accountId != resourceAccountId || clientId != resourceClientId)
            throw new UnauthorizedAccessException("O recurso não pertence ao portal autenticado.");
    }

    public void EnsurePartnerAccess(Guid accountId, Guid resourceAccountId, Guid partnerId, Guid assignedPartnerId)
    {
        if (accountId == Guid.Empty || partnerId == Guid.Empty || accountId != resourceAccountId || partnerId != assignedPartnerId)
            throw new UnauthorizedAccessException("O recurso não está atribuído ao parceiro autenticado.");
    }
}

public static class FinancialFlowPolicy
{
    public static void EnsureReceiptAllowed(PaymentStatus status)
    {
        if (status != PaymentStatus.Approved) throw new InvalidOperationException("Somente pagamento confirmado pode gerar recibo.");
    }
}

public sealed record RefinementCheck(string Name, bool Passed);
public sealed record ModuleRefinementScore(int Score, int Passed, int Total);
public sealed class ModuleRefinementScoreService
{
    public ModuleRefinementScore Calculate(IEnumerable<RefinementCheck> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var checks = source.ToArray();
        if (checks.Length == 0) return new(0, 0, 0);
        var passed = checks.Count(x => x.Passed);
        return new((int)decimal.Round(passed * 100m / checks.Length, 0, MidpointRounding.AwayFromZero), passed, checks.Length);
    }
}

public enum JourneyKind { Commercial, Operational, Financial, Fiscal, Projects, Assets, Portals, Design }
public sealed record JourneyRefinementResult(
    JourneyKind Journey, IReadOnlyList<ModuleReadiness> Modules, ModuleRefinementScore RuleScore,
    int OpenFindings, int CriticalFindings, string RecommendedAction);

/// <summary>Projects the live source audit into journey-level, deterministic quality results.</summary>
public sealed class UserJourneyReviewService(FunctionalQualityService quality, ModuleRefinementScoreService scores)
{
    private static readonly IReadOnlyDictionary<JourneyKind, string[]> Modules = new Dictionary<JourneyKind, string[]>
    {
        [JourneyKind.Commercial] = ["Comercial", "Clientes", "Orçamentos", "Propostas", "Precificação"],
        [JourneyKind.Operational] = ["OS", "Campo", "Manutenção"],
        [JourneyKind.Financial] = ["Financeiro", "Pagamentos", "Contratos"],
        [JourneyKind.Fiscal] = ["Financeiro", "Pagamentos"],
        [JourneyKind.Projects] = ["Projetos", "Contratos"],
        [JourneyKind.Assets] = ["Ativos", "Manutenção", "Campo"],
        [JourneyKind.Portals] = ["Portal do Cliente", "Portal do Parceiro"],
        [JourneyKind.Design] = ["Admin", "Relatórios", "Suporte", "Omnichannel"]
    };

    public IReadOnlyList<JourneyRefinementResult> Review(DateTimeOffset now)
    {
        var snapshot = quality.Evaluate(now);
        return Modules.Select(entry =>
        {
            var modules = snapshot.Modules.Where(x => entry.Value.Contains(x.Module, StringComparer.OrdinalIgnoreCase)).ToArray();
            var checks = modules.SelectMany(x => x.Checks).Select(x => new RefinementCheck(x.Name, x.Passed));
            var score = scores.Calculate(checks);
            var moduleFolders = modules.Select(x => x.Route.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()).Where(x => x is not null).ToArray();
            var findings = snapshot.Findings.Where(x => moduleFolders.Any(folder => x.File.Contains($"/{folder}/", StringComparison.OrdinalIgnoreCase))).ToArray();
            var action = findings.FirstOrDefault()?.Recommendation
                ?? modules.FirstOrDefault(x => x.Status != QualityStatus.Ready)?.Recommendation
                ?? "Manter os controles automatizados e revisar a jornada após cada mudança.";
            return new JourneyRefinementResult(entry.Key, modules, score, findings.Length,
                findings.Count(x => x.Severity is FindingSeverity.P0 or FindingSeverity.P1), action);
        }).ToArray();
    }
}

public sealed class FriendlyErrorMessageService
{
    private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["required_checklist"] = "Não foi possível concluir a OS porque ainda existem itens obrigatórios no checklist.",
        ["expired_proposal"] = "A proposta expirou. Reabra uma nova versão antes de solicitar a aprovação.",
        ["pending_payment"] = "O recibo estará disponível após a confirmação do pagamento.",
        ["fiscal_profile"] = "Complete os dados fiscais da empresa e do cliente antes de emitir a nota.",
        ["duplicate_operation"] = "Esta operação já foi registrada. Atualize a página para consultar o resultado."
    };

    public string Get(string code) => Messages.TryGetValue(code, out var message)
        ? message
        : "Não foi possível concluir esta ação. Revise os dados informados e tente novamente.";
}

public static class JourneyRuleValidationService
{
    public static void EnsureQuoteCanAdvance(Guid clientId, int itemCount)
    {
        if (clientId == Guid.Empty) throw new ArgumentException("Selecione um cliente antes de avançar.", nameof(clientId));
        if (itemCount <= 0) throw new InvalidOperationException("Adicione pelo menos um item válido ao orçamento antes de avançar.");
    }

    public static void EnsureProposalCanApprove(DateOnly validUntil, DateOnly today, bool reopened)
    {
        if (validUntil < today && !reopened) throw new InvalidOperationException("A proposta expirou e precisa ser reaberta em uma nova versão.");
    }
}
