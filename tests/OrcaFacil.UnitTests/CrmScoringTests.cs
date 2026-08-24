using OrcaFacil.Application.Crm;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.UnitTests;

public sealed class CrmScoringTests
{
    [Fact] public void Health_score_is_deterministic_and_explainable()
    {
        var input = new HealthScoreInput(true, 2, true, true, 3, 10, 0, 20, 5);
        var first = ClientHealthScoreService.Calculate(input); var second = ClientHealthScoreService.Calculate(input);
        Assert.Equal(first.Score, second.Score); Assert.Equal(first.Factors, second.Factors); Assert.Equal(100, first.Score); Assert.NotEmpty(first.Factors);
    }

    [Fact] public void Critical_retention_risk_lists_real_reasons()
    {
        var result = RetentionRiskService.Evaluate(new(90, true, true, 3, true, 10, false, true));
        Assert.Equal(RetentionRiskLevel.Critical, result.Level); Assert.Contains(result.Reasons, x => x.Contains("Pagamento"));
    }

    [Fact] public void Nps_requires_real_responses_and_uses_standard_formula()
    {
        Assert.Null(NpsService.Calculate(Array.Empty<int>()));
        Assert.Equal(0m, NpsService.Calculate(new[] { 10, 8, 2 }));
    }

    [Fact] public void Campaign_respects_consent_and_never_auto_sends_whatsapp()
    {
        Assert.False(CampaignConsentService.CanSendCommercial(true, true, false));
        Assert.False(CampaignConsentService.CanDispatchAutomatically(CampaignChannel.WhatsApp, true));
        Assert.False(CampaignConsentService.CanDispatchAutomatically(CampaignChannel.Email, false));
    }
}
