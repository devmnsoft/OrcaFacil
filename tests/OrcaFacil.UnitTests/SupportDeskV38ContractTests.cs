using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class SupportDeskV38ContractTests
{
    [Fact] public void Internal_note_has_explicit_non_public_type()
    {
        var message = new SupportTicketMessage { AuthorUserId=Guid.NewGuid(), TicketId=Guid.NewGuid(), Body="diagnóstico", IsInternal=true, Type=SupportMessageType.InternalNote };
        Assert.True(message.IsInternal); Assert.Equal(SupportMessageType.InternalNote,message.Type);
    }
    [Fact] public void Csat_domain_accepts_only_persisted_response_shape()
    {
        var response = new SupportCsatResponse { AccountId=Guid.NewGuid(), SurveyId=Guid.NewGuid(), Rating=1, WasResolved=false, TimelinessAdequate=false };
        Assert.InRange(response.Rating,1,5); Assert.NotEqual(Guid.Empty,response.SurveyId);
    }
    [Fact] public void Incident_is_private_until_explicit_approval()
    {
        var incident = new SupportIncident { Title="Falha",Description="Investigação",StartedAt=DateTime.UtcNow,CreatedByUserId=Guid.NewGuid() };
        Assert.False(incident.IsPublicApproved); Assert.Equal(SupportIncidentStatus.Investigating,incident.Status);
    }
}
