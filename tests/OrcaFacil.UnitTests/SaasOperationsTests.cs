using OrcaFacil.Application.Jobs;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class SaasOperationsTests
{
    [Fact]
    public void Active_job_lock_cannot_be_acquired_twice()
    {
        var jobLock = new JobLock("email-outbox");
        var now = DateTime.UtcNow;
        Assert.True(jobLock.TryAcquire("worker-a", now, TimeSpan.FromMinutes(2)));
        Assert.False(jobLock.TryAcquire("worker-b", now.AddSeconds(1), TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Expired_job_lock_can_be_reused()
    {
        var jobLock = new JobLock("usage-snapshot");
        var now = DateTime.UtcNow;
        jobLock.TryAcquire("worker-a", now, TimeSpan.FromSeconds(1));
        Assert.True(jobLock.TryAcquire("worker-b", now.AddSeconds(2), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Completed_outbox_item_is_not_reprocessed()
    {
        var item = NewOutbox();
        Assert.True(item.Start("worker-a", DateTime.UtcNow.AddSeconds(1)));
        item.Complete();
        Assert.False(item.Start("worker-b", DateTime.UtcNow.AddMinutes(1)));
    }

    [Fact]
    public void Outbox_respects_maximum_attempts()
    {
        var item = NewOutbox(maximumAttempts: 1);
        item.Start("worker-a", DateTime.UtcNow.AddSeconds(1));
        item.Fail("falha sanitizada", DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(OutboxStatus.Failed, item.Status);
        Assert.False(item.Start("worker-a", DateTime.UtcNow.AddMinutes(2)));
    }

    [Fact]
    public void Quota_blocks_only_new_creation_at_limit()
    {
        var decision = new QuotaService().CheckCreation("propostas públicas", 10, 10);
        Assert.False(decision.Allowed);
        Assert.Contains("upgrade", decision.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tenant_cache_key_never_omits_account()
    {
        var accountA = Guid.NewGuid(); var accountB = Guid.NewGuid();
        Assert.NotEqual(TenantCacheKey.Create(accountA, "permissions", "user"), TenantCacheKey.Create(accountB, "permissions", "user"));
        Assert.Throws<ArgumentException>(() => TenantCacheKey.Create(Guid.Empty, "permissions", "user"));
    }

    private static ProcessingOutboxItem NewOutbox(int maximumAttempts = 5) => new(Guid.NewGuid(), "email", "invoice:1", "{}", maximumAttempts: maximumAttempts);
}
