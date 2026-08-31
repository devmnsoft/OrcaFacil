using OrcaFacil.Application.Quality;

namespace OrcaFacil.UnitTests;

public sealed class CommercialJourneyTests
{
    [Fact]
    public void Quote_without_items_cannot_advance() =>
        Assert.Throws<InvalidOperationException>(() => JourneyRuleValidationService.EnsureQuoteCanAdvance(Guid.NewGuid(), 0));

    [Fact]
    public void Expired_proposal_cannot_be_approved_without_reopening() =>
        Assert.Throws<InvalidOperationException>(() => JourneyRuleValidationService.EnsureProposalCanApprove(new(2026, 8, 30), new(2026, 8, 31), false));
}

public sealed class FriendlyErrorMessageTests
{
    [Theory]
    [InlineData("required_checklist", "checklist")]
    [InlineData("pending_payment", "pagamento")]
    [InlineData("fiscal_profile", "fiscais")]
    public void Known_error_has_actionable_message(string code, string expected) =>
        Assert.Contains(expected, new FriendlyErrorMessageService().Get(code), StringComparison.OrdinalIgnoreCase);
}
