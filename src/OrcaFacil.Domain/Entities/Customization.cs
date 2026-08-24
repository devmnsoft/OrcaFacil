using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum CustomFieldType { ShortText, LongText, Number, Currency, Percentage, Date, DateTime, Boolean, SingleChoice, MultipleChoice, Email, Phone, Url, Document, File }

public sealed class CustomFieldDefinition : Entity
{
    private CustomFieldDefinition() { }
    public CustomFieldDefinition(Guid accountId, string entityType, string code, string label, CustomFieldType fieldType)
    {
        AccountId = accountId; EntityType = entityType; Code = code.Trim().ToLowerInvariant(); Label = label.Trim(); FieldType = fieldType;
    }
    public Guid AccountId { get; private set; }
    public string EntityType { get; private set; } = "";
    public string Code { get; private set; } = "";
    public string Label { get; private set; } = "";
    public string? Description { get; set; }
    public CustomFieldType FieldType { get; private set; }
    public bool IsRequired { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsVisibleInList { get; set; }
    public bool IsVisibleInPortal { get; set; }
    public bool IsSensitive { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationJson { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CustomFieldValue : Entity
{
    private CustomFieldValue() { }
    public CustomFieldValue(Guid accountId, Guid definitionId, string entityType, Guid entityId)
    { AccountId = accountId; CustomFieldDefinitionId = definitionId; EntityType = entityType; EntityId = entityId; }
    public Guid AccountId { get; private set; }
    public Guid CustomFieldDefinitionId { get; private set; }
    public string EntityType { get; private set; } = "";
    public Guid EntityId { get; private set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public DateTime? ValueDate { get; set; }
    public bool? ValueBool { get; set; }
    public string? ValueJson { get; set; }
}

public sealed class DynamicFormDefinition : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = "";
    public string FormType { get; set; } = "";
    public string TargetEntityType { get; set; } = "";
    public bool IsPublished { get; set; }
    public Guid? CurrentVersionId { get; set; }
}
public sealed class DynamicFormVersion : Entity { public Guid FormDefinitionId { get; set; } public int VersionNumber { get; set; } public string Title { get; set; } = ""; public string? SchemaJson { get; set; } public DateTime? PublishedAt { get; set; } }
public sealed class DynamicFormSubmission : Entity { public Guid AccountId { get; set; } public Guid FormDefinitionId { get; set; } public Guid FormVersionId { get; set; } public string TargetEntityType { get; set; } = ""; public Guid? TargetEntityId { get; set; } public Guid? SubmittedByUserId { get; set; } public Guid? SubmittedByPortalUserId { get; set; } public Guid? SubmittedByPartnerUserId { get; set; } public string Status { get; set; } = "Draft"; public DateTime? SubmittedAt { get; set; } }

public sealed class WorkflowDefinition : Entity { public Guid AccountId { get; set; } public string EntityType { get; set; } = ""; public string Name { get; set; } = ""; public bool IsActive { get; set; } public Guid? CurrentVersionId { get; set; } }
public sealed class WorkflowVersion : Entity { public Guid WorkflowDefinitionId { get; set; } public int VersionNumber { get; set; } public DateTime? PublishedAt { get; set; } }
public sealed class WorkflowState : Entity { public Guid AccountId { get; set; } public Guid WorkflowVersionId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsInitial { get; set; } public bool IsFinal { get; set; } public bool IsCanceled { get; set; } public int DisplayOrder { get; set; } }
public sealed class WorkflowTransition : Entity { public Guid AccountId { get; set; } public Guid WorkflowVersionId { get; set; } public string FromStateCode { get; set; } = ""; public string ToStateCode { get; set; } = ""; public string Name { get; set; } = ""; public string? RequiresPermission { get; set; } public bool RequiresApproval { get; set; } public bool RequiresComment { get; set; } public bool RequiresConfirmation { get; set; } }
public sealed class WorkflowInstance : Entity { public Guid AccountId { get; set; } public Guid WorkflowVersionId { get; set; } public string EntityType { get; set; } = ""; public Guid EntityId { get; set; } public string CurrentStateCode { get; set; } = ""; }
public sealed class WorkflowInstanceEvent : Entity { public Guid AccountId { get; set; } public Guid WorkflowInstanceId { get; set; } public string FromStateCode { get; set; } = ""; public string ToStateCode { get; set; } = ""; public Guid ActorUserId { get; set; } public string? Comment { get; set; } public string CorrelationId { get; set; } = ""; }

public sealed class AutomationRuleDefinition : Entity { public Guid AccountId { get; set; } public string Name { get; set; } = ""; public string Trigger { get; set; } = ""; public string ConditionsJson { get; set; } = "[]"; public string ActionsJson { get; set; } = "[]"; public bool IsActive { get; set; } }
public sealed class AutomationRuleRun : Entity { public Guid AccountId { get; set; } public Guid AutomationRuleDefinitionId { get; set; } public string EventId { get; set; } = ""; public string Status { get; set; } = "Pending"; public string CorrelationId { get; set; } = ""; public string? ErrorSummary { get; set; } }

public sealed class ChecklistTemplate : Entity { public Guid AccountId { get; set; } public string Name { get; set; } = ""; public string TargetEntityType { get; set; } = ""; public bool IsDefault { get; set; } public bool IsActive { get; set; } = true; public int Version { get; set; } = 1; }
public sealed class ChecklistTemplateItem : Entity { public Guid AccountId { get; set; } public Guid ChecklistTemplateId { get; set; } public string Title { get; set; } = ""; public bool IsRequired { get; set; } public bool RequiresEvidence { get; set; } public int DisplayOrder { get; set; } }
public sealed class ConfigurablePipeline : Entity { public Guid AccountId { get; set; } public string EntityType { get; set; } = ""; public string Name { get; set; } = ""; public bool IsDefault { get; set; } public bool IsActive { get; set; } = true; }
public sealed class ConfigurablePipelineStage : Entity { public Guid AccountId { get; set; } public Guid PipelineId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public int DisplayOrder { get; set; } public bool IsInitial { get; set; } public bool IsFinal { get; set; } public bool IsCanceled { get; set; } }
