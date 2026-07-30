using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Persistence.Services;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class CommercialJourneyInterfaceContractTests
{
    [Theory]
    [InlineData(nameof(ICommercialJourneyService.CreateRevisionAsync), typeof(RevisionResult))]
    [InlineData(nameof(ICommercialJourneyService.CreatePublicAccessAsync), typeof(PublicQuoteResult))]
    [InlineData(nameof(ICommercialJourneyService.DecideAsync), typeof(PublicDecisionResult))]
    public void Quote_methods_use_the_canonical_document_result(string methodName, Type resultType)
    {
        Assert.Contains(typeof(ICommercialJourneyService), typeof(CommercialJourneyService).GetInterfaces());
        var contract = Assert.Single(typeof(ICommercialJourneyService).GetMethods().Where(x => x.Name == methodName));
        var implementation = typeof(CommercialJourneyService).GetMethod(methodName);

        Assert.NotNull(implementation);
        Assert.Equal(typeof(Task<>).MakeGenericType(resultType), contract.ReturnType);
        Assert.Equal(contract.ReturnType, implementation!.ReturnType);
        Assert.Equal(typeof(CancellationToken), contract.GetParameters()[^1].ParameterType);
        Assert.Equal(contract.GetParameters().Select(x => x.ParameterType),
            implementation.GetParameters().Select(x => x.ParameterType));
    }
}
