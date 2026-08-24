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

public sealed class Sprint27ApiKeySecurityTests
{
    [Xunit.Fact]
    public void Created_key_is_returned_once_but_only_hash_is_persistable()
    {
        var service = new OrcaFacil.Application.Integrations.ApiKeyService();
        var created = service.Create(Guid.NewGuid(), Guid.NewGuid(), "ERP", ["clients.read"], DateTime.UtcNow.AddDays(1));
        Xunit.Assert.StartsWith("of_live_", created.PlaintextKey);
        Xunit.Assert.DoesNotContain(created.PlaintextKey, created.Entity.KeyHash);
        Xunit.Assert.True(service.Verify(created.PlaintextKey, created.Entity));
    }

    [Xunit.Fact]
    public void Revoked_and_expired_keys_do_not_verify()
    {
        var service = new OrcaFacil.Application.Integrations.ApiKeyService();
        var revoked = service.Create(Guid.NewGuid(), Guid.NewGuid(), "ERP", ["clients.read"], null);
        revoked.Entity.RevokedAt = DateTime.UtcNow;
        Xunit.Assert.False(service.Verify(revoked.PlaintextKey, revoked.Entity));
        var expired = service.Create(Guid.NewGuid(), Guid.NewGuid(), "ERP", ["clients.read"], DateTime.UtcNow.AddMinutes(1));
        expired.Entity.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        Xunit.Assert.False(service.Verify(expired.PlaintextKey, expired.Entity));
    }
}
