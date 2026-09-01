using OrcaFacil.Application.Automation;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class AutomationEngineTests
{
    private static AutomationRuleBuilderService Builder() => new(new AutomationTriggerCatalogService(),new AutomationConditionCatalogService(),new AutomationActionCatalogService());
    private static AutomationRuleDraft Draft(string trigger="proposal.viewed", params string[] actions) => new(Guid.NewGuid(),"Follow-up seguro",null,trigger,[],actions.Select(x=>new AutomationAction(x,new Dictionary<string,string>())).ToArray(),Guid.NewGuid());

    [Fact] public void Rule_without_trigger_cannot_be_published() => Assert.False(Builder().Validate(Draft("","followup.create")).IsValid);
    [Fact] public void Rule_without_action_cannot_be_published() => Assert.False(Builder().Validate(Draft()).IsValid);
    [Fact] public void Critical_action_requires_human_approval() => Assert.True(Builder().Validate(Draft("payment.confirmed","payment.confirm")).RequiresApproval);
    [Fact] public void Trigger_catalog_has_known_typed_payloads() { var items=new AutomationTriggerCatalogService().Get(); Assert.Equal(33,items.Count); Assert.All(items,x=>Assert.NotEmpty(x.Payload)); }
    [Fact] public void Money_is_compared_as_decimal()
    {
        var evaluator=new AutomationConditionEvaluator(new AutomationConditionCatalogService());
        var result=evaluator.Evaluate(new("value.gt","amount","10.50"),new Dictionary<string,object?>{{"amount",10.51m}});
        Assert.True(result.Matched);
    }
    [Fact] public void Sensitive_condition_is_denied_without_permission()
    {
        var evaluator=new AutomationConditionEvaluator(new AutomationConditionCatalogService());
        Assert.False(evaluator.Evaluate(new("margin.below_minimum","margin","20"),new Dictionary<string,object?>{{"margin",10m}}).Matched);
    }
    [Fact] public void Dry_run_blocks_critical_action_and_does_not_execute_it()
    {
        var dryRun=new AutomationDryRunService(Builder(),new(new AutomationConditionCatalogService()),new());
        var result=dryRun.Simulate(Draft("payment.confirmed","payment.confirm"),new Dictionary<string,object?>());
        Assert.Empty(result.Actions); Assert.Contains("payment.confirm",result.BlockedActions); Assert.True(result.RequiresApproval);
    }
    [Fact] public void Duplicate_event_is_not_queued_twice()
    {
        var queue=new AutomationEventQueueService(); var item=new AutomationEvent(Guid.NewGuid(),"proposal.viewed","proposal:42",new Dictionary<string,object?>());
        Assert.True(queue.Enqueue(item)); Assert.False(queue.Enqueue(item));
    }
    [Fact] public void Worker_retry_uses_bounded_exponential_backoff() => Assert.True(AutomationEventQueueService.RetryDelay(5)>AutomationEventQueueService.RetryDelay(2));
    [Fact] public void Requester_cannot_approve_own_critical_action()
    {
        var user=Guid.NewGuid(); var request=new AutomationApproval(Guid.NewGuid(),Guid.NewGuid(),user,"payment.confirm",DateTimeOffset.UtcNow,"Pending");
        Assert.Throws<InvalidOperationException>(()=>new AutomationApprovalService().Decide(request,user,true,true,null));
    }
    [Fact] public void Rejection_requires_reason()
    {
        var request=new AutomationApproval(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"payment.confirm",DateTimeOffset.UtcNow,"Pending");
        Assert.Throws<ArgumentException>(()=>new AutomationApprovalService().Decide(request,Guid.NewGuid(),true,false,null));
    }
    [Fact] public void Template_always_creates_account_draft()
    {
        var account=Guid.NewGuid(); var draft=new AutomationTemplateService().CreateDraft("proposal-viewed-followup",account,Guid.NewGuid());
        Assert.Equal(account,draft.AccountId); Assert.Equal("proposal.viewed",draft.TriggerCode);
    }
}
