namespace OrcaFacil.Application.CustomerSuccess;

public static class CustomerSuccessPermissions
{
    public const string View = "CustomerSuccess.View";
    public const string Manage = "CustomerSuccess.Manage";
    public const string AccountsView = "CustomerSuccess.AccountsView";
    public const string HealthScoreView = "CustomerSuccess.HealthScoreView";
    public const string HealthRulesManage = "CustomerSuccess.HealthRulesManage";
    public const string ChurnRiskView = "CustomerSuccess.ChurnRiskView";
    public const string RetentionPlansView = "CustomerSuccess.RetentionPlansView";
    public const string RetentionPlansManage = "CustomerSuccess.RetentionPlansManage";
    public const string ExpansionView = "CustomerSuccess.ExpansionView";
    public const string ExpansionManage = "CustomerSuccess.ExpansionManage";
    public const string RenewalsView = "CustomerSuccess.RenewalsView";
    public const string RenewalsManage = "CustomerSuccess.RenewalsManage";
    public const string NpsView = "CustomerSuccess.NpsView";
    public const string NpsManage = "CustomerSuccess.NpsManage";
    public const string QbrView = "CustomerSuccess.QbrView";
    public const string QbrManage = "CustomerSuccess.QbrManage";
    public const string PlaybooksView = "CustomerSuccess.PlaybooksView";
    public const string PlaybooksManage = "CustomerSuccess.PlaybooksManage";
    public const string TouchpointsView = "CustomerSuccess.TouchpointsView";
    public const string TouchpointsManage = "CustomerSuccess.TouchpointsManage";
    public const string SuccessPlansView = "CustomerSuccess.SuccessPlansView";
    public const string SuccessPlansManage = "CustomerSuccess.SuccessPlansManage";
    public const string AdoptionView = "CustomerSuccess.AdoptionView";
    public const string EscalationsView = "CustomerSuccess.EscalationsView";
    public const string EscalationsManage = "CustomerSuccess.EscalationsManage";
    public const string AlertsView = "CustomerSuccess.AlertsView";
    public const string ReportsView = "CustomerSuccess.ReportsView";
    public const string FinancialSensitiveView = "CustomerSuccess.FinancialSensitiveView";
}

public enum CustomerHealthBand { InsufficientData, Critical, Attention, Healthy, Excellent }
public enum ChurnRiskLevel { Low, Medium, High, Critical }
public enum RetentionPlanStatus { Draft, Active, WaitingCustomer, WaitingInternal, Recovered, Lost, Canceled }
public enum NpsClassification { Detractor, Neutral, Promoter }

public sealed record CustomerSignal(string Code, decimal Value, string Source, DateTimeOffset ObservedAt);
public sealed record HealthRule(string Code, decimal Weight, decimal GoodThreshold, bool HigherIsBetter, bool Active = true);
public sealed record HealthFactor(string Code, decimal ObservedValue, decimal Contribution, decimal Weight, string Source);
public sealed record CustomerHealthScore(Guid AccountId, decimal? Score, CustomerHealthBand Band, IReadOnlyList<HealthFactor> Factors, DateTimeOffset CalculatedAt);

public sealed class CustomerHealthRuleService
{
    public HealthRule Create(string code, decimal weight, decimal threshold, bool higherIsBetter)
    {
        if (string.IsNullOrWhiteSpace(code) || weight <= 0 || weight > 100) throw new ArgumentException("Código e peso entre 0 e 100 são obrigatórios.");
        return new(code.Trim(), weight, threshold, higherIsBetter);
    }
}

public sealed class CustomerHealthScoreService
{
    public CustomerHealthScore Calculate(Guid accountId, IReadOnlyList<CustomerSignal> signals, IReadOnlyList<HealthRule> rules)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("A conta é obrigatória.");
        var active = rules.Where(x => x.Active).ToArray();
        var factors = active.Join(signals, r => r.Code, s => s.Code, (r, s) =>
        {
            var met = r.HigherIsBetter ? s.Value >= r.GoodThreshold : s.Value <= r.GoodThreshold;
            return new HealthFactor(r.Code, s.Value, met ? r.Weight : 0, r.Weight, s.Source);
        }).ToArray();
        if (active.Length == 0 || factors.Length < Math.Max(2, active.Length / 2))
            return new(accountId, null, CustomerHealthBand.InsufficientData, factors, DateTimeOffset.UtcNow);
        var score = decimal.Round(factors.Sum(x => x.Contribution) / active.Sum(x => x.Weight) * 100, 2);
        var band = score switch { < 40 => CustomerHealthBand.Critical, < 60 => CustomerHealthBand.Attention, < 80 => CustomerHealthBand.Healthy, _ => CustomerHealthBand.Excellent };
        return new(accountId, score, band, factors, DateTimeOffset.UtcNow);
    }
}

public sealed record ChurnRiskFactor(string Code, int Points, string Reason, string Source);
public sealed record CustomerChurnRisk(Guid AccountId, ChurnRiskLevel Level, IReadOnlyList<ChurnRiskFactor> Factors);
public sealed class CustomerChurnRiskService
{
    public CustomerChurnRisk Assess(Guid accountId, IEnumerable<ChurnRiskFactor> observedFactors)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("A conta é obrigatória.");
        var factors = observedFactors.Where(x => x.Points > 0 && !string.IsNullOrWhiteSpace(x.Source) && !string.IsNullOrWhiteSpace(x.Reason)).ToArray();
        var points = factors.Sum(x => x.Points);
        var level = points switch { >= 80 => ChurnRiskLevel.Critical, >= 50 => ChurnRiskLevel.High, >= 25 => ChurnRiskLevel.Medium, _ => ChurnRiskLevel.Low };
        return new(accountId, level, factors);
    }
}

public sealed record RetentionAction(string Description, Guid OwnerId, DateOnly DueDate, bool Completed = false);
public sealed record RetentionPlan(Guid Id, Guid AccountId, Guid OwnerId, string Reason, string Objective, IReadOnlyList<RetentionAction> Actions, RetentionPlanStatus Status, string? Result = null, string? LossReason = null);
public sealed class CustomerRetentionPlanService
{
    public RetentionPlan Create(Guid accountId, Guid ownerId, string reason, string objective, IReadOnlyList<RetentionAction> actions)
    {
        if (accountId == Guid.Empty || ownerId == Guid.Empty || actions.Count == 0 || actions.Any(x => x.OwnerId == Guid.Empty)) throw new ArgumentException("Conta, responsável e ao menos uma ação com prazo são obrigatórios.");
        return new(Guid.NewGuid(), accountId, ownerId, reason.Trim(), objective.Trim(), actions, RetentionPlanStatus.Draft);
    }
    public RetentionPlan Close(RetentionPlan plan, RetentionPlanStatus status, string outcome) => status switch
    {
        RetentionPlanStatus.Recovered when !string.IsNullOrWhiteSpace(outcome) => plan with { Status = status, Result = outcome.Trim() },
        RetentionPlanStatus.Lost when !string.IsNullOrWhiteSpace(outcome) => plan with { Status = status, LossReason = outcome.Trim() },
        _ => throw new ArgumentException("Encerramento recuperado ou perdido exige resultado/motivo.")
    };
}

public sealed record ExpansionOpportunity(Guid Id, Guid AccountId, string Type, string Origin, decimal? EstimatedValue, bool ConfirmedForCrm = false);
public sealed class CustomerExpansionOpportunityService
{
    public ExpansionOpportunity Create(Guid accountId, string type, string origin, decimal? estimatedValue)
    {
        if (accountId == Guid.Empty || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(origin) || estimatedValue < 0) throw new ArgumentException("Conta, tipo e origem real são obrigatórios.");
        return new(Guid.NewGuid(), accountId, type.Trim(), origin.Trim(), estimatedValue);
    }
}

public sealed record RenewalCycle(Guid Id, Guid AccountId, Guid ContractId, DateOnly RenewalDate, string Stage, bool Approved);
public sealed class CustomerRenewalService
{
    public RenewalCycle Create(Guid accountId, Guid contractId, DateOnly renewalDate, DateOnly today)
    {
        if (accountId == Guid.Empty || contractId == Guid.Empty) throw new ArgumentException("Conta e contrato são obrigatórios.");
        var days = renewalDate.DayNumber - today.DayNumber;
        var stage = days switch { < 0 => "Overdue", <= 7 => "D-7", <= 15 => "D-15", <= 30 => "D-30", <= 60 => "D-60", _ => "D-90" };
        return new(Guid.NewGuid(), accountId, contractId, renewalDate, stage, false);
    }
}

public sealed record NpsSurvey(Guid Id, Guid AccountId, Guid ContactId, string Token, bool Answered);
public sealed record NpsResponse(Guid SurveyId, int Score, string? Comment, NpsClassification Classification, DateTimeOffset AnsweredAt, bool FollowUpRequired);
public sealed class CustomerNpsService
{
    public NpsResponse Respond(NpsSurvey survey, int score, string? comment)
    {
        if (survey.Answered) throw new InvalidOperationException("Esta pesquisa já foi respondida.");
        if (score is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(score));
        var classification = score <= 6 ? NpsClassification.Detractor : score <= 8 ? NpsClassification.Neutral : NpsClassification.Promoter;
        return new(survey.Id, score, comment?.Trim(), classification, DateTimeOffset.UtcNow, classification == NpsClassification.Detractor);
    }
}

public sealed record CustomerQbr(Guid Id, Guid AccountId, Guid OwnerId, DateOnly PeriodStart, DateOnly PeriodEnd, string ExecutiveSummary, string? InternalNotes, decimal? SensitiveRevenue);
public sealed class CustomerQbrService
{
    public CustomerQbr Create(Guid accountId, Guid ownerId, DateOnly start, DateOnly end, string summary, string? internalNotes, decimal? revenue)
    {
        if (accountId == Guid.Empty || ownerId == Guid.Empty || end < start) throw new ArgumentException("Cliente, responsável e período válido são obrigatórios.");
        return new(Guid.NewGuid(), accountId, ownerId, start, end, summary.Trim(), internalNotes, revenue);
    }
    public CustomerQbr ForPortal(CustomerQbr qbr) => qbr with { InternalNotes = null, SensitiveRevenue = null };
    public CustomerQbr ForUser(CustomerQbr qbr, IReadOnlySet<string> permissions) => permissions.Contains(CustomerSuccessPermissions.FinancialSensitiveView) ? qbr : qbr with { SensitiveRevenue = null };
}

public sealed record PlaybookStep(string Code, string Title, string ActionType, bool Critical);
public sealed record SuccessPlaybook(Guid Id, Guid AccountId, string Name, int Version, IReadOnlyList<PlaybookStep> Steps, bool Published);
public sealed record PlaybookRun(Guid Id, Guid AccountId, Guid PlaybookId, int PlaybookVersion, IReadOnlyList<PlaybookStep> StepsSnapshot);
public sealed class CustomerSuccessPlaybookService
{
    public SuccessPlaybook Publish(SuccessPlaybook playbook)
    {
        if (playbook.Steps.Count == 0) throw new InvalidOperationException("O playbook exige etapas.");
        return playbook with { Version = playbook.Version + 1, Published = true };
    }
}
public sealed class CustomerSuccessPlaybookRunService
{
    public PlaybookRun Start(SuccessPlaybook playbook, Guid accountId)
    {
        if (!playbook.Published || accountId == Guid.Empty) throw new InvalidOperationException("Somente versão publicada pode ser executada.");
        return new(Guid.NewGuid(), accountId, playbook.Id, playbook.Version, playbook.Steps.ToArray());
    }
}

public sealed record CustomerTouchpoint(Guid Id, Guid AccountId, Guid OwnerId, string Type, string Notes, DateTimeOffset OccurredAt);
public sealed class CustomerTouchpointService
{
    public CustomerTouchpoint Register(Guid accountId, Guid ownerId, string type, string notes, DateTimeOffset occurredAt)
    {
        if (accountId == Guid.Empty || ownerId == Guid.Empty || string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Cliente, responsável e tipo são obrigatórios.");
        return new(Guid.NewGuid(), accountId, ownerId, type.Trim(), notes.Trim(), occurredAt);
    }
}

public sealed record CustomerSuccessAlert(Guid Id, Guid AccountId, string RuleCode, string Reason, string Link, bool Resolved);
public sealed class CustomerSuccessAlertService
{
    public CustomerSuccessAlert Add(ICollection<CustomerSuccessAlert> alerts, Guid accountId, string ruleCode, string reason, string link)
    {
        if (accountId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || !link.StartsWith('/')) throw new ArgumentException("Conta, motivo e link interno real são obrigatórios.");
        var existing = alerts.FirstOrDefault(x => x.AccountId == accountId && x.RuleCode == ruleCode && !x.Resolved);
        if (existing is not null) return existing;
        var alert = new CustomerSuccessAlert(Guid.NewGuid(), accountId, ruleCode, reason.Trim(), link, false);
        alerts.Add(alert); return alert;
    }
}

public sealed class CustomerSuccessTenantIsolationService
{
    public IReadOnlyList<T> ForAccount<T>(IEnumerable<T> records, Guid accountId, Func<T, Guid> accountSelector)
    {
        if (accountId == Guid.Empty) throw new UnauthorizedAccessException("Escopo da conta não identificado.");
        return records.Where(x => accountSelector(x) == accountId).ToArray();
    }
}

public sealed record CustomerSuccessAccount(Guid AccountId, Guid ClientId, Guid? OwnerId, CustomerHealthScore Health, CustomerChurnRisk ChurnRisk, decimal? Mrr);
public sealed class CustomerSuccessAccountService(CustomerSuccessTenantIsolationService isolation)
{
    public IReadOnlyList<CustomerSuccessAccount> List(IEnumerable<CustomerSuccessAccount> accounts, Guid accountId, IReadOnlySet<string> permissions) =>
        isolation.ForAccount(accounts, accountId, x => x.AccountId)
            .Select(x => permissions.Contains(CustomerSuccessPermissions.FinancialSensitiveView) ? x : x with { Mrr = null }).ToArray();
}

public sealed record SuccessPlanGoal(Guid Id, Guid OwnerId, string Objective, string MetricSource, decimal Target, decimal? Realized);
public sealed record CustomerSuccessPlan(Guid Id, Guid AccountId, Guid ClientId, IReadOnlyList<SuccessPlanGoal> Goals, string? InternalNotes, bool SharedWithPortal);
public sealed class CustomerSuccessPlanService
{
    public CustomerSuccessPlan Create(Guid accountId, Guid clientId, IReadOnlyList<SuccessPlanGoal> goals, string? internalNotes)
    {
        if (accountId == Guid.Empty || clientId == Guid.Empty || goals.Count == 0 || goals.Any(x => x.OwnerId == Guid.Empty || string.IsNullOrWhiteSpace(x.MetricSource)))
            throw new ArgumentException("Cliente e objetivos com responsável e origem de métrica são obrigatórios.");
        return new(Guid.NewGuid(), accountId, clientId, goals, internalNotes, false);
    }
    public CustomerSuccessPlan ForPortal(CustomerSuccessPlan plan) => plan with { InternalNotes = null };
}

public sealed record AdoptionSnapshot(Guid AccountId, Guid ClientId, DateOnly Date, IReadOnlyDictionary<string, long> Metrics, string Source);
public sealed class CustomerAdoptionAnalyticsService
{
    public AdoptionSnapshot Build(Guid accountId, Guid clientId, DateOnly date, IReadOnlyDictionary<string, long> metrics, string source)
    {
        if (accountId == Guid.Empty || clientId == Guid.Empty || metrics.Count == 0 || metrics.Values.Any(x => x < 0) || string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("A adoção exige cliente, métricas não negativas e origem real.");
        return new(accountId, clientId, date, metrics, source.Trim());
    }
}

public sealed record CustomerSuccessEscalation(Guid Id, Guid AccountId, Guid ClientId, Guid OwnerId, string Reason, string Severity, string? Result);
public sealed class CustomerSuccessEscalationService
{
    public CustomerSuccessEscalation Open(Guid accountId, Guid clientId, Guid ownerId, string reason, string severity)
    {
        if (accountId == Guid.Empty || clientId == Guid.Empty || ownerId == Guid.Empty || string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Cliente, responsável e motivo são obrigatórios.");
        return new(Guid.NewGuid(), accountId, clientId, ownerId, reason.Trim(), severity, null);
    }
    public CustomerSuccessEscalation Close(CustomerSuccessEscalation escalation, string result) => string.IsNullOrWhiteSpace(result)
        ? throw new ArgumentException("O encerramento exige resultado.") : escalation with { Result = result.Trim() };
}

public sealed class CustomerSuccessReportService(CustomerSuccessTenantIsolationService isolation)
{
    public IReadOnlyList<CustomerSuccessAccount> Accounts(IEnumerable<CustomerSuccessAccount> source, Guid accountId, DateOnly periodStart, DateOnly periodEnd, IReadOnlySet<string> permissions)
    {
        if (periodEnd < periodStart) throw new ArgumentException("O relatório exige período válido.");
        return isolation.ForAccount(source, accountId, x => x.AccountId)
            .Select(x => permissions.Contains(CustomerSuccessPermissions.FinancialSensitiveView) ? x : x with { Mrr = null }).ToArray();
    }
}
