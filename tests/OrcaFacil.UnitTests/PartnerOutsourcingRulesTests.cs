using OrcaFacil.Application.Outsourcing;
using OrcaFacil.Application.Partners;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.UnitTests;

public sealed class PartnerOutsourcingRulesTests
{
    [Fact]
    public void Partner_token_is_random_and_only_hash_is_persistable()
    {
        var service = new PartnerTokenService(); var issued = service.Issue(TimeSpan.FromMinutes(15));
        Assert.NotEqual(issued.RawToken, issued.TokenHash); Assert.Equal(64, issued.TokenHash.Length); Assert.True(service.Matches(issued.RawToken, issued.TokenHash));
    }

    [Fact]
    public void Invitation_cannot_be_reused_or_accepted_after_expiration()
    {
        var invitation = new PartnerPortalInvitation { AcceptedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(1), TokenHash = new string('0', 64) };
        Assert.Throws<InvalidOperationException>(() => PartnerPortalInvitationService.ValidateForAcceptance(invitation, "token", new PartnerTokenService(), DateTime.UtcNow));
    }

    [Fact]
    public void Partner_cannot_access_another_tenant_or_partner()
    {
        Assert.Throws<UnauthorizedAccessException>(() => PartnerAccessService.Demand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Expired_quote_cannot_be_accepted()
    {
        var account = Guid.NewGuid(); var partner = new PartnerProfile { AccountId = account, Status = PartnerStatus.Active };
        var quote = new OutsourcingQuote { AccountId = account, PartnerId = partner.Id, Status = OutsourcingQuoteStatus.Submitted, ExpiresAt = DateTime.UtcNow.AddMinutes(-1) };
        Assert.Throws<InvalidOperationException>(() => OutsourcingRules.ValidateQuoteAcceptance(quote, partner, account, DateTime.UtcNow));
    }

    [Fact]
    public void Payment_request_never_starts_paid()
    {
        var account = Guid.NewGuid(); var partner = Guid.NewGuid(); var assignment = new OutsourcingAssignment { AccountId = account, PartnerId = partner, WorkOrderId = Guid.NewGuid() };
        var payment = new PartnerPaymentRequest { AccountId = account, PartnerId = partner, WorkOrderId = assignment.WorkOrderId, OutsourcingAssignmentId = assignment.Id, Amount = 10, Status = PartnerPaymentStatus.Paid };
        Assert.Throws<InvalidOperationException>(() => OutsourcingRules.ValidatePaymentRequest(payment, assignment));
    }

    [Fact]
    public void Rating_uses_real_scores_and_validates_scale()
    {
        var rating = new PartnerRating { QualityScore=5, PunctualityScore=4, CommunicationScore=3, DeadlineScore=4, DocumentationScore=5, CostBenefitScore=4, ClientSatisfactionScore=3 };
        Assert.Equal(4.00m, OutsourcingRules.RatingAverage(rating));
    }
}
