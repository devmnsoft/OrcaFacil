using Microsoft.Extensions.DependencyInjection;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Persistence.Services;
using Xunit;
namespace OrcaFacil.UnitTests;

public sealed class CommercialJourneyDependencyInjectionTests
{
    [Fact]
    public void CommercialContracts_ResolveToOneScopedInstance()
    {
        using var factory = new RouteApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var concrete = scope.ServiceProvider.GetRequiredService<CommercialJourneyService>();
        var journey = scope.ServiceProvider.GetRequiredService<ICommercialJourneyService>();
        var payments = scope.ServiceProvider.GetRequiredService<IManualPaymentRegistrationService>();

        Assert.Same(concrete, journey);
        Assert.Same(concrete, payments);
    }
}
