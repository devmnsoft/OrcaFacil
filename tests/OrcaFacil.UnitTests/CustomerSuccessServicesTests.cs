using OrcaFacil.Application.CustomerSuccess;

namespace OrcaFacil.UnitTests;

public sealed class CustomerSuccessServicesTests
{
    private readonly Guid _account = Guid.NewGuid();

    [Fact]
    public void Health_score_uses_traceable_signals_and_reports_insufficient_data()
    {
        var service = new CustomerHealthScoreService();
        var rules = new[] { new HealthRule("logins", 50, 5, true), new HealthRule("tickets", 50, 2, false) };
        var insufficient = service.Calculate(_account, [new("logins", 8, "usage_events", DateTimeOffset.UtcNow)], rules);
        Assert.Equal(CustomerHealthBand.InsufficientData, insufficient.Band);
        var result = service.Calculate(_account, [new("logins", 8, "usage_events", DateTimeOffset.UtcNow), new("tickets", 1, "support_tickets", DateTimeOffset.UtcNow)], rules);
        Assert.Equal(100, result.Score); Assert.All(result.Factors, x => Assert.False(string.IsNullOrWhiteSpace(x.Source)));
    }

    [Fact]
    public void Churn_risk_is_explainable()
    {
        var risk = new CustomerChurnRiskService().Assess(_account, [new("low-use", 55, "Sem login há 30 dias", "usage_events")]);
        Assert.Equal(ChurnRiskLevel.High, risk.Level); Assert.Single(risk.Factors);
    }

    [Fact]
    public void Nps_rejects_duplicate_and_requires_follow_up_for_detractor()
    {
        var service = new CustomerNpsService();
        var response = service.Respond(new(Guid.NewGuid(), _account, Guid.NewGuid(), "token", false), 4, "Preciso de ajuda");
        Assert.True(response.FollowUpRequired); Assert.Equal(NpsClassification.Detractor, response.Classification);
        Assert.Throws<InvalidOperationException>(() => service.Respond(new(Guid.NewGuid(), _account, Guid.NewGuid(), "token", true), 10, null));
    }

    [Fact]
    public void Retention_requires_owner_and_actions()
    {
        var service = new CustomerRetentionPlanService();
        Assert.Throws<ArgumentException>(() => service.Create(_account, Guid.NewGuid(), "risco", "reter", []));
        var owner = Guid.NewGuid();
        var plan = service.Create(_account, owner, "baixo uso", "recuperar adoção", [new("Treinamento", owner, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))]);
        Assert.Single(plan.Actions);
    }

    [Fact]
    public void Qbr_hides_internal_and_financial_data()
    {
        var service = new CustomerQbrService();
        var qbr = service.Create(_account, Guid.NewGuid(), new(2026, 1, 1), new(2026, 3, 31), "Resumo", "nota interna", 1000);
        var portal = service.ForPortal(qbr);
        Assert.Null(portal.InternalNotes); Assert.Null(portal.SensitiveRevenue);
    }

    [Fact]
    public void Playbook_run_preserves_published_version_snapshot()
    {
        var template = new SuccessPlaybook(Guid.NewGuid(), _account, "Baixo uso", 0, [new("task", "Revisar adoção", "CreateTask", false)], false);
        var published = new CustomerSuccessPlaybookService().Publish(template);
        var run = new CustomerSuccessPlaybookRunService().Start(published, _account);
        Assert.Equal(1, run.PlaybookVersion); Assert.Single(run.StepsSnapshot);
    }

    [Fact]
    public void Tenant_filter_never_returns_another_account()
    {
        var other = Guid.NewGuid();
        var records = new[] { new CustomerSuccessAlert(Guid.NewGuid(), _account, "a", "reason", "/a", false), new CustomerSuccessAlert(Guid.NewGuid(), other, "b", "reason", "/b", false) };
        var result = new CustomerSuccessTenantIsolationService().ForAccount(records, _account, x => x.AccountId);
        Assert.Single(result); Assert.Equal(_account, result[0].AccountId);
    }
}
