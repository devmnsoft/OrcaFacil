using OrcaFacil.Application.Integrations;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class IntegrationSecurityTests
{
    [Fact]
    public void Api_key_is_only_returned_once_and_entity_keeps_hash()
    {
        var service = new ApiKeyService();
        var result = service.Create(Guid.NewGuid(), Guid.NewGuid(), "ERP", ["clients.read"], DateTime.UtcNow.AddDays(1));
        Assert.StartsWith("of_live_", result.PlaintextKey);
        Assert.DoesNotContain(result.PlaintextKey, result.Entity.KeyHash);
        Assert.True(service.Verify(result.PlaintextKey, result.Entity));
    }

    [Fact]
    public void Api_key_rejects_unknown_scope()
    {
        var service = new ApiKeyService();
        Assert.Throws<ArgumentException>(() => service.Create(Guid.NewGuid(), Guid.NewGuid(), "ERP", ["admin.all"], null));
    }
}
