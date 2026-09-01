using System.Collections.Concurrent;
using System.Globalization;

namespace OrcaFacil.Application.Automation;

public enum AutomationRisk { Low, Medium, High, Critical }
public enum AutomationExecutionStatus { Queued, Running, Succeeded, PartiallySucceeded, Failed, Skipped, Blocked, WaitingApproval, DeadLetter, Canceled }
public enum ConditionJoin { And, Or }

public sealed record TriggerDefinition(string Code, string Name, string Module, bool IsAsync, bool AllowsCriticalActions, IReadOnlyDictionary<string, Type> Payload, string Example, bool Active = true);
public sealed record ConditionDefinition(string Code, string Name, Type ValueType, bool Sensitive = false);
public sealed record ActionDefinition(string Code, string Name, string Module, AutomationRisk Risk, bool CreatesRecord = false, bool RequiresProvider = false);
public sealed record AutomationCondition(string Code, string Field, string? Value);
public sealed record AutomationConditionGroup(ConditionJoin Join, IReadOnlyList<AutomationCondition> Conditions);
public sealed record AutomationAction(string Code, IReadOnlyDictionary<string, string> Parameters);
public sealed record AutomationRuleDraft(Guid AccountId, string Name, string? Description, string TriggerCode, IReadOnlyList<AutomationConditionGroup> ConditionGroups, IReadOnlyList<AutomationAction> Actions, Guid OwnerId, bool IsGlobal = false);
public sealed record AutomationValidationResult(bool IsValid, IReadOnlyList<string> Errors, bool RequiresApproval, AutomationRisk HighestRisk);
public sealed record ConditionEvaluation(string Code, bool Matched, string Message);
public sealed record DryRunResult(Guid SimulationId, bool WouldRun, IReadOnlyList<ConditionEvaluation> Conditions, IReadOnlyList<string> Actions, IReadOnlyList<string> BlockedActions, bool RequiresApproval, IReadOnlyList<string> Logs);

public sealed class AutomationTriggerCatalogService
{
    private static readonly IReadOnlyList<TriggerDefinition> Items = Build();
    public IReadOnlyList<TriggerDefinition> Get(string? module = null) => Items.Where(x => x.Active && (module is null || x.Module.Equals(module, StringComparison.OrdinalIgnoreCase))).ToArray();
    public TriggerDefinition? Find(string code) => Items.SingleOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    private static IReadOnlyList<TriggerDefinition> Build()
    {
        var idStatus = new Dictionary<string, Type> { ["entityId"] = typeof(Guid), ["status"] = typeof(string), ["occurredAt"] = typeof(DateTimeOffset) };
        var specs = new (string Code,string Name,string Module)[] {
            ("lead.created","Lead criado","CRM"),("lead.unassigned","Lead sem responsável","CRM"),("opportunity.stale","Oportunidade parada","CRM"),("client.created","Cliente criado","Clientes"),("quote.created","Orçamento criado","Comercial"),("quote.internally_approved","Orçamento aprovado internamente","Comercial"),("proposal.sent","Proposta enviada","Comercial"),("proposal.viewed","Proposta visualizada","Comercial"),("proposal.approved","Proposta aprovada","Comercial"),("proposal.rejected","Proposta recusada","Comercial"),("proposal.expired","Proposta expirada","Comercial"),("work_order.created","OS criada","Operações"),("work_order.scheduled","OS agendada","Operações"),("work_order.overdue","OS atrasada","Operações"),("work_order.completed","OS concluída","Operações"),("work_order.review_pending","OS pendente de revisão","Operações"),("payment.confirmed","Pagamento confirmado","Financeiro"),("invoice.overdue","Fatura vencida","Financeiro"),("receipt.issued","Recibo emitido","Financeiro"),("contract.expiring","Contrato vencendo","Contratos"),("contract.expired","Contrato vencido","Contratos"),("asset.maintenance_overdue","Ativo com manutenção vencida","Ativos"),("inspection.completed","Inspeção concluída","Qualidade"),("nonconformity.critical","Não conformidade crítica criada","Qualidade"),("action_plan.overdue","Plano de ação atrasado","Qualidade"),("ticket.created","Ticket criado","Suporte"),("ticket.sla_breached","Ticket SLA violado","Suporte"),("conversation.received","Conversa recebida","Omnichannel"),("project.overdue","Projeto atrasado","Projetos"),("milestone.accepted","Marco aceito","Projetos"),("task.overdue","Tarefa atrasada","Tarefas"),("fiscal.rejected","Documento fiscal rejeitado","Fiscal"),("webhook.received","Webhook recebido","Integrações") };
        return specs.Select(x => new TriggerDefinition(x.Code,x.Name,x.Module,true,x.Code is "payment.confirmed" or "milestone.accepted",idStatus,$"Quando {x.Name.ToLowerInvariant()}.")).ToArray();
    }
}

public sealed class AutomationConditionCatalogService
{
    private static readonly IReadOnlyList<ConditionDefinition> Items = new ConditionDefinition[] {
        new("status.eq","Status igual a",typeof(string)),new("status.ne","Status diferente de",typeof(string)),new("value.gt","Valor maior que",typeof(decimal)),new("value.lt","Valor menor que",typeof(decimal)),new("date.overdue","Data vencida",typeof(DateTimeOffset)),new("days_without_update","Dias sem atualização",typeof(int)),new("client.has_plan","Cliente possui plano",typeof(bool)),new("client.active_contract","Cliente possui contrato ativo",typeof(bool)),new("client.overdue","Cliente inadimplente",typeof(bool)),new("user.in_role","Usuário pertence ao perfil",typeof(string)),new("priority.eq","Prioridade igual a",typeof(string)),new("margin.below_minimum","Margem abaixo do mínimo",typeof(decimal),true),new("sla.breached","SLA violado",typeof(bool)),new("outside_business_hours","Fora do horário comercial",typeof(bool)),new("channel.eq","Canal igual a",typeof(string)),new("origin.eq","Origem igual a",typeof(string)),new("tag.contains","Tag contém",typeof(string)),new("custom.eq","Campo customizado igual a",typeof(string)) };
    public IReadOnlyList<ConditionDefinition> Get(bool canViewSensitive) => Items.Where(x => !x.Sensitive || canViewSensitive).ToArray();
    public ConditionDefinition? Find(string code) => Items.SingleOrDefault(x => x.Code.Equals(code,StringComparison.OrdinalIgnoreCase));
}

public sealed class AutomationActionCatalogService
{
    private static readonly IReadOnlyList<ActionDefinition> Items = new ActionDefinition[] {
        new("task.create","Criar tarefa","Tarefas",AutomationRisk.Low,true),new("alert.create","Criar alerta","Alertas",AutomationRisk.Low,true),new("followup.create","Criar follow-up","CRM",AutomationRisk.Low,true),new("owner.assign","Atribuir responsável","Geral",AutomationRisk.Medium),new("tag.add","Adicionar tag","Geral",AutomationRisk.Low),new("status.move","Mover status","Geral",AutomationRisk.Medium),new("comment.create","Criar comentário interno","Geral",AutomationRisk.Low,true),new("email.draft","Criar rascunho de e-mail","Comunicação",AutomationRisk.Low,true),new("whatsapp.draft","Criar rascunho de WhatsApp","Comunicação",AutomationRisk.Low,true),new("ticket.create","Criar ticket","Suporte",AutomationRisk.Low,true),new("lead.create","Criar lead","CRM",AutomationRisk.Low,true),new("preventive_work_order.create","Criar OS preventiva","Ativos",AutomationRisk.Medium,true),new("action_plan.create","Criar plano de ação","Qualidade",AutomationRisk.Medium,true),new("approval.request","Solicitar aprovação","Aprovações",AutomationRisk.Medium,true),new("user.notify","Notificar usuário interno","Notificações",AutomationRisk.Low),new("team.notify","Notificar equipe","Notificações",AutomationRisk.Low),new("audit.create","Criar evento de auditoria","Auditoria",AutomationRisk.Low,true),new("webhook.call","Chamar webhook configurado","Integrações",AutomationRisk.High,false,true),new("report.schedule","Gerar relatório agendado","Relatórios",AutomationRisk.Medium,true),new("automation.pause","Pausar automação por erro","Automação",AutomationRisk.Low),
        new("payment.confirm","Confirmar pagamento","Financeiro",AutomationRisk.Critical),new("receipt.issue","Emitir recibo","Financeiro",AutomationRisk.Critical),new("fiscal.issue","Emitir documento fiscal","Fiscal",AutomationRisk.Critical),new("fiscal.cancel","Cancelar documento fiscal","Fiscal",AutomationRisk.Critical),new("permission.change","Alterar permissão","Segurança",AutomationRisk.Critical),new("data.delete","Excluir dado","Privacidade",AutomationRisk.Critical),new("finance.close","Fechar mês financeiro","Financeiro",AutomationRisk.Critical) };
    public IReadOnlyList<ActionDefinition> Get() => Items;
    public ActionDefinition? Find(string code) => Items.SingleOrDefault(x => x.Code.Equals(code,StringComparison.OrdinalIgnoreCase));
}

public sealed class AutomationConditionEvaluator(AutomationConditionCatalogService catalog)
{
    public ConditionEvaluation Evaluate(AutomationCondition condition, IReadOnlyDictionary<string, object?> payload, bool canViewSensitive = false)
    {
        var definition = catalog.Find(condition.Code);
        if (definition is null || (definition.Sensitive && !canViewSensitive)) return new(condition.Code,false,"Condição indisponível ou sem permissão.");
        if (!payload.TryGetValue(condition.Field,out var actual)) return new(condition.Code,false,"Campo não encontrado no payload.");
        try
        {
            var expected = ConvertValue(condition.Value, definition.ValueType);
            var matched = condition.Code switch {
                "status.ne" => !EqualsInvariant(actual, expected),
                "value.gt" => Convert.ToDecimal(actual,CultureInfo.InvariantCulture) > (decimal)expected!,
                "value.lt" or "margin.below_minimum" => Convert.ToDecimal(actual,CultureInfo.InvariantCulture) < (decimal)expected!,
                "date.overdue" => DateTimeOffset.Parse(Convert.ToString(actual,CultureInfo.InvariantCulture)!,CultureInfo.InvariantCulture) < (DateTimeOffset)expected!,
                "days_without_update" => Convert.ToInt32(actual,CultureInfo.InvariantCulture) >= (int)expected!,
                "tag.contains" => Convert.ToString(actual,CultureInfo.InvariantCulture)?.Split(',').Any(x => x.Trim().Equals(condition.Value,StringComparison.OrdinalIgnoreCase)) == true,
                _ => EqualsInvariant(actual,expected)
            };
            return new(condition.Code,matched,matched ? "Condição atendida." : "Condição não atendida.");
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return new(condition.Code,false,"Valor incompatível com o tipo da condição."); }
    }
    private static object? ConvertValue(string? value, Type type) => type == typeof(string) ? value : type == typeof(decimal) ? decimal.Parse(value!,NumberStyles.Number,CultureInfo.InvariantCulture) : type == typeof(int) ? int.Parse(value!,CultureInfo.InvariantCulture) : type == typeof(bool) ? bool.Parse(value!) : type == typeof(DateTimeOffset) ? DateTimeOffset.Parse(value!,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal) : value;
    private static bool EqualsInvariant(object? left, object? right) => string.Equals(Convert.ToString(left,CultureInfo.InvariantCulture),Convert.ToString(right,CultureInfo.InvariantCulture),StringComparison.OrdinalIgnoreCase);
}

public sealed record AutomationSafetyPolicy(int MaxExecutionsPerHour = 100, int MaxConsecutiveFailures = 5, bool RequireApprovalForCriticalActions = true, bool BlockOutsideBusinessHours = false);
public sealed class AutomationSafetyPolicyService
{
    public AutomationSafetyPolicy MostRestrictive(AutomationSafetyPolicy account, AutomationSafetyPolicy? global) => global is null ? account : new(Math.Min(account.MaxExecutionsPerHour,global.MaxExecutionsPerHour),Math.Min(account.MaxConsecutiveFailures,global.MaxConsecutiveFailures),account.RequireApprovalForCriticalActions || global.RequireApprovalForCriticalActions,account.BlockOutsideBusinessHours || global.BlockOutsideBusinessHours);
}

public sealed class AutomationRuleBuilderService(AutomationTriggerCatalogService triggers, AutomationConditionCatalogService conditions, AutomationActionCatalogService actions)
{
    public AutomationValidationResult Validate(AutomationRuleDraft rule, bool isSuperAdmin = false, bool canViewSensitive = false)
    {
        var errors = new List<string>();
        if (rule.AccountId == Guid.Empty && !(rule.IsGlobal && isSuperAdmin)) errors.Add("A conta é obrigatória.");
        if (string.IsNullOrWhiteSpace(rule.Name)) errors.Add("Informe o nome da automação.");
        var trigger = triggers.Find(rule.TriggerCode);
        if (trigger is null || !trigger.Active) errors.Add("Selecione um gatilho ativo.");
        if (rule.Actions.Count == 0) errors.Add("Adicione pelo menos uma ação.");
        foreach (var condition in rule.ConditionGroups.SelectMany(x => x.Conditions)) { var definition = conditions.Find(condition.Code); if (definition is null || (definition.Sensitive && !canViewSensitive) || string.IsNullOrWhiteSpace(condition.Field)) errors.Add($"Condição inválida: {condition.Code}."); }
        var definitions = rule.Actions.Select(x => actions.Find(x.Code)).ToArray();
        if (definitions.Any(x => x is null)) errors.Add("A regra contém uma ação inválida.");
        var highest = definitions.Where(x => x is not null).Select(x => x!.Risk).DefaultIfEmpty(AutomationRisk.Low).Max();
        var requiresApproval = highest == AutomationRisk.Critical || (trigger is not null && !trigger.AllowsCriticalActions && highest >= AutomationRisk.High);
        return new(errors.Count == 0,errors,requiresApproval,highest);
    }
}

public sealed class AutomationDryRunService(AutomationRuleBuilderService builder, AutomationConditionEvaluator evaluator, AutomationActionCatalogService actions)
{
    public DryRunResult Simulate(AutomationRuleDraft rule, IReadOnlyDictionary<string, object?> payload, bool canViewSensitive = false)
    {
        var validation = builder.Validate(rule,canViewSensitive:canViewSensitive);
        if (!validation.IsValid) return new(Guid.NewGuid(),false,[],[],rule.Actions.Select(x => x.Code).ToArray(),validation.RequiresApproval,validation.Errors);
        var evaluations = rule.ConditionGroups.SelectMany(g => g.Conditions.Select(c => evaluator.Evaluate(c,payload,canViewSensitive))).ToArray();
        var matches = rule.ConditionGroups.All(g => g.Conditions.Count == 0 || (g.Join == ConditionJoin.And ? g.Conditions.Select(c => evaluator.Evaluate(c,payload,canViewSensitive)).All(x => x.Matched) : g.Conditions.Select(c => evaluator.Evaluate(c,payload,canViewSensitive)).Any(x => x.Matched)));
        var blocked = rule.Actions.Where(a => actions.Find(a.Code)?.Risk == AutomationRisk.Critical).Select(a => a.Code).ToArray();
        var executable = matches ? rule.Actions.Select(x => x.Code).Except(blocked).ToArray() : [];
        return new(Guid.NewGuid(),matches,evaluations,executable,blocked,validation.RequiresApproval,["Simulação concluída sem persistir alterações ou chamar provedores."]);
    }
}

public sealed record AutomationEvent(Guid AccountId, string TriggerCode, string IdempotencyKey, IReadOnlyDictionary<string, object?> Payload, int Attempt = 0);
public sealed class AutomationEventQueueService
{
    private readonly ConcurrentDictionary<string,AutomationEvent> _events = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<AutomationEvent> _queue = new();
    public bool Enqueue(AutomationEvent item) { if (item.AccountId == Guid.Empty || string.IsNullOrWhiteSpace(item.IdempotencyKey)) throw new ArgumentException("Conta e chave de idempotência são obrigatórias."); var key=$"{item.AccountId:N}:{item.IdempotencyKey}"; if (!_events.TryAdd(key,item)) return false; _queue.Enqueue(item); return true; }
    public bool TryDequeue(out AutomationEvent? item) => _queue.TryDequeue(out item);
    public static TimeSpan RetryDelay(int attempt) => TimeSpan.FromSeconds(Math.Min(300,Math.Pow(2,Math.Clamp(attempt,1,8))));
}

public sealed record AutomationApproval(Guid Id, Guid AccountId, Guid RequesterId, string ActionCode, DateTimeOffset RequestedAt, string Status, string? Reason = null, Guid? DecidedBy = null);
public sealed class AutomationApprovalService
{
    public AutomationApproval Decide(AutomationApproval request, Guid approverId, bool authorized, bool approve, string? reason)
    {
        if (!authorized || request.AccountId == Guid.Empty) throw new UnauthorizedAccessException("Aprovação não autorizada.");
        if (request.RequesterId == approverId) throw new InvalidOperationException("O solicitante não pode aprovar a própria ação crítica.");
        if (!approve && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Informe o motivo da reprovação.");
        return request with { Status = approve ? "Approved" : "Rejected", Reason = reason?.Trim(), DecidedBy = approverId };
    }
}

public sealed record AutomationTemplate(string Code,string Name,string Module,string TriggerCode,string ActionCode);
public sealed class AutomationTemplateService
{
    private static readonly AutomationTemplate[] Templates = [new("proposal-viewed-followup","Follow-up de proposta visualizada","Comercial","proposal.viewed","followup.create"),new("proposal-no-response","Alerta de proposta sem resposta","Comercial","opportunity.stale","alert.create"),new("approved-proposal-work-order","Criar OS após proposta aprovada","Operações","proposal.approved","preventive_work_order.create"),new("work-order-review","Solicitar revisão após OS concluída","Qualidade","work_order.completed","approval.request"),new("overdue-invoice-alert","Gerar alerta de fatura vencida","Financeiro","invoice.overdue","alert.create"),new("overdue-maintenance","Criar manutenção preventiva vencida","Ativos","asset.maintenance_overdue","preventive_work_order.create"),new("sla-escalation","Escalar ticket com SLA violado","Suporte","ticket.sla_breached","owner.assign"),new("critical-nonconformity","Criar plano de ação para não conformidade crítica","Qualidade","nonconformity.critical","action_plan.create"),new("overdue-project","Alertar projeto atrasado","Projetos","project.overdue","alert.create"),new("milestone-acceptance","Solicitar aceite de marco","Projetos","milestone.accepted","approval.request")];
    public IReadOnlyList<AutomationTemplate> Get() => Templates;
    public AutomationRuleDraft CreateDraft(string code,Guid accountId,Guid ownerId) { var template=Templates.Single(x=>x.Code==code); return new(accountId,template.Name,null,template.TriggerCode,[],[new(template.ActionCode,new Dictionary<string,string>())],ownerId); }
}
