using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence.Services;

namespace OrcaFacil.UnitTests;

public sealed class CommercialJourneyInterfaceContractTests
{
    [Fact]
    public void Service_ImplementsCanonicalQuoteLifecycleContract()
    {
        Assert.Contains(typeof(ICommercialJourneyService), typeof(CommercialJourneyService).GetInterfaces());
        AssertMethod(nameof(ICommercialJourneyService.CreateRevisionAsync), typeof(Task<RevisionResult>),
            typeof(Guid), typeof(string), typeof(CancellationToken));
        AssertMethod(nameof(ICommercialJourneyService.CreatePublicAccessAsync), typeof(Task<PublicQuoteResult>),
            typeof(Guid), typeof(TimeSpan), typeof(CancellationToken));
        AssertMethod(nameof(ICommercialJourneyService.DecideAsync), typeof(Task<PublicDecisionResult>),
            typeof(string), typeof(PublicDocumentDecisionType), typeof(string), typeof(string), typeof(string),
            typeof(string), typeof(string), typeof(string), typeof(CancellationToken));
    }

    private static void AssertMethod(string name, Type returnType, params Type[] parameters)
    {
        var contract = typeof(ICommercialJourneyService).GetMethod(name, parameters);
        var implementation = typeof(CommercialJourneyService).GetMethod(name, parameters);
        Assert.NotNull(contract);
        Assert.NotNull(implementation);
        Assert.Equal(returnType, contract.ReturnType);
        Assert.Equal(returnType, implementation.ReturnType);
        Assert.Equal(typeof(CancellationToken), contract.GetParameters()[^1].ParameterType);
    }
}
