using OrcaFacil.Application.Customization;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

public class CustomizationRulesTests
{
    [Fact]
    public void Required_custom_field_is_validated_and_tenant_isolated()
    {
        var account = Guid.NewGuid();
        var field = new CustomFieldDefinition(account, "WorkOrder", "serial", "Número de série", CustomFieldType.ShortText) { IsRequired = true };
        Assert.False(CustomFieldValueService.Validate(field, account, null).Success);
        Assert.False(CustomFieldValueService.Validate(field, Guid.NewGuid(), "123").Success);
    }

    [Fact]
    public void Sensitive_or_non_portal_field_is_not_disclosed()
    {
        var account = Guid.NewGuid();
        var field = new CustomFieldDefinition(account, "Client", "risk", "Risco", CustomFieldType.ShortText) { IsSensitive = true, IsVisibleInPortal = false };
        Assert.False(CustomFieldValueService.CanRead(field, account, false, false));
        Assert.False(CustomFieldValueService.CanRead(field, account, true, true));
    }

    [Fact]
    public void Workflow_rejects_forbidden_transition_and_missing_comment()
    {
        var account = Guid.NewGuid();
        var transition = new WorkflowTransition { AccountId = account, FromStateCode = "open", ToStateCode = "done", RequiresComment = true };
        Assert.False(WorkflowExecutionService.Validate(transition, new(account, "open", "canceled", true, true, true, "motivo")).Success);
        Assert.False(WorkflowExecutionService.Validate(transition, new(account, "open", "done", true, true, true, null)).Success);
    }

    [Fact]
    public void Automation_is_idempotent_per_account_rule_and_event()
    {
        var account = Guid.NewGuid();
        var rule = new AutomationRuleDefinition { AccountId = account, IsActive = true };
        var run = new AutomationRuleRun { AccountId = account, AutomationRuleDefinitionId = rule.Id, EventId = "event-1" };
        Assert.False(AutomationRuleEngine.ShouldRun(rule, account, "event-1", new[] { run }));
        Assert.True(AutomationRuleEngine.ShouldRun(rule, account, "event-2", new[] { run }));
    }

    [Fact]
    public void Financial_actions_are_not_allowed()
    {
        Assert.False(WorkflowActionService.Validate("registerPayment").Success);
        Assert.False(WorkflowActionService.Validate("issueReceipt").Success);
        Assert.True(WorkflowActionService.Validate("notify").Success);
    }
}
