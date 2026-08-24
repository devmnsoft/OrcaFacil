using System.Globalization;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Customization;

public sealed record RuleResult(bool Success, string? Error = null)
{
    public static RuleResult Ok() => new(true);
    public static RuleResult Fail(string error) => new(false, error);
}

public static class CustomFieldValueService
{
    public static RuleResult Validate(CustomFieldDefinition definition, Guid accountId, string? value)
    {
        if (definition.AccountId != accountId) return RuleResult.Fail("O campo não pertence a esta conta.");
        if (!definition.IsActive) return RuleResult.Fail("O campo está inativo.");
        if (definition.IsRequired && string.IsNullOrWhiteSpace(value)) return RuleResult.Fail($"{definition.Label} é obrigatório.");
        if (string.IsNullOrWhiteSpace(value)) return RuleResult.Ok();
        return definition.FieldType switch
        {
            CustomFieldType.Number or CustomFieldType.Currency or CustomFieldType.Percentage when !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _) => RuleResult.Fail($"{definition.Label} deve ser numérico."),
            CustomFieldType.Date or CustomFieldType.DateTime when !System.DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _) => RuleResult.Fail($"{definition.Label} deve conter uma data válida."),
            CustomFieldType.Boolean when !bool.TryParse(value, out _) => RuleResult.Fail($"{definition.Label} deve ser verdadeiro ou falso."),
            CustomFieldType.File => RuleResult.Fail("Arquivos devem ser vinculados por um FileAsset seguro."),
            _ => RuleResult.Ok()
        };
    }

    public static bool CanRead(CustomFieldDefinition definition, Guid accountId, bool canReadSensitive, bool portal)
        => definition.AccountId == accountId && !definition.IsDeleted && (!portal || definition.IsVisibleInPortal) && (!definition.IsSensitive || canReadSensitive);
}

public static class DynamicFormService
{
    public static DynamicFormVersion Publish(DynamicFormDefinition form, Guid accountId, IEnumerable<DynamicFormVersion> existing, string schemaJson)
    {
        if (form.AccountId != accountId) throw new InvalidOperationException("Formulário não pertence a esta conta.");
        if (string.IsNullOrWhiteSpace(schemaJson)) throw new InvalidOperationException("O formulário precisa conter campos válidos.");
        var version = new DynamicFormVersion { FormDefinitionId = form.Id, VersionNumber = existing.Select(x => x.VersionNumber).DefaultIfEmpty(0).Max() + 1, Title = form.Name, SchemaJson = schemaJson, PublishedAt = DateTime.UtcNow };
        form.CurrentVersionId = version.Id; form.IsPublished = true; form.Touch();
        return version;
    }
}

public sealed record TransitionRequest(Guid AccountId, string From, string To, bool HasPermission, bool IsApproved, bool Confirmed, string? Comment);
public static class WorkflowExecutionService
{
    public static RuleResult Validate(WorkflowTransition transition, TransitionRequest request)
    {
        if (transition.AccountId != request.AccountId) return RuleResult.Fail("Transição não pertence a esta conta.");
        if (!string.Equals(transition.FromStateCode, request.From, StringComparison.OrdinalIgnoreCase) || !string.Equals(transition.ToStateCode, request.To, StringComparison.OrdinalIgnoreCase)) return RuleResult.Fail("Transição não permitida para o estado atual.");
        if (transition.RequiresPermission is not null && !request.HasPermission) return RuleResult.Fail("Você não possui permissão para esta transição.");
        if (transition.RequiresApproval && !request.IsApproved) return RuleResult.Fail("Esta transição exige aprovação.");
        if (transition.RequiresConfirmation && !request.Confirmed) return RuleResult.Fail("Confirme a transição antes de continuar.");
        if (transition.RequiresComment && string.IsNullOrWhiteSpace(request.Comment)) return RuleResult.Fail("Informe um comentário para continuar.");
        return RuleResult.Ok();
    }
}

public static class WorkflowConditionService
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "equals", "greaterThan", "lessThan", "status", "permission", "unit", "team", "assignee", "marginBelow", "valueAbove", "delinquent", "slaBreached", "checklistPending", "attachmentMissing", "customFieldEquals" };
    public static RuleResult ValidateType(string type) => Allowed.Contains(type) ? RuleResult.Ok() : RuleResult.Fail("Tipo de condição não permitido.");
    public static bool Evaluate(string operation, decimal left, decimal right) => operation switch { "greaterThan" => left > right, "lessThan" => left < right, "equals" => left == right, _ => false };
}

public static class WorkflowActionService
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "notify", "createTask", "createAlert", "recommend", "changeStage", "requestApproval", "applyChecklist", "timeline", "enqueueEmail", "enqueueWebhook", "addTag" };
    public static RuleResult Validate(string action) => Allowed.Contains(action) ? RuleResult.Ok() : RuleResult.Fail("Ação automática não permitida por segurança.");
}

public static class AutomationRuleEngine
{
    public static bool ShouldRun(AutomationRuleDefinition rule, Guid accountId, string eventId, IEnumerable<AutomationRuleRun> runs)
        => rule.AccountId == accountId && rule.IsActive && !runs.Any(x => x.AccountId == accountId && x.AutomationRuleDefinitionId == rule.Id && x.EventId == eventId);
}

public static class ChecklistRules
{
    public static RuleResult CanComplete(IEnumerable<(bool Required, bool Completed, bool RequiresEvidence, Guid? EvidenceId)> items)
        => items.Any(x => x.Required && (!x.Completed || x.RequiresEvidence && x.EvidenceId is null)) ? RuleResult.Fail("Conclua os itens obrigatórios e anexe as evidências exigidas.") : RuleResult.Ok();
}
