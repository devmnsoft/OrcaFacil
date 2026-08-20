using OrcaFacil.Application.Scoring;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialScoringTests
{
    [Fact] public void Quote_score_is_deterministic_and_explainable()
    {
        var service = new QuoteScoreService();
        var input = new QuoteScoreInput(15000, true, true, 2, 1, 2, false);
        var first = service.Calculate(input); var second = service.Calculate(input);
        Assert.Equal(first, second); Assert.InRange(first.Value, 0, 100); Assert.NotEmpty(first.Reasons);
    }

    [Fact] public void Overdue_client_is_classified_from_real_receivable_value()
    {
        var score = new ClientScoreService().Calculate(new(20000, 10000, 3, 2, 100, 2, 1));
        Assert.Equal("Cliente inadimplente", score.Classification); Assert.Contains("vencidos", score.Explanation);
    }
}
