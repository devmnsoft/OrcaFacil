using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
using OrcaFacil.Application.Security;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task RegisterAsync_tracks_account_and_user_without_saving()
    {
        await using var db = new OrcaFacilDbContext(new DbContextOptionsBuilder<OrcaFacilDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options);
        var service = new AuditService(db, new SensitiveDataSanitizer());
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await service.RegisterAsync(userId, "ACCOUNT_REGISTERED", nameof(BusinessAccount), accountId.ToString(),
            null, new { accountId }, null, accountId: accountId);

        var entry = Assert.Single(db.ChangeTracker.Entries<AuditLog>());
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal(accountId, entry.Entity.AccountId);
        Assert.Equal(userId, entry.Entity.UserId);
        Assert.NotEqual(Guid.Empty, entry.Entity.CorrelationId);
        Assert.DoesNotContain("accountId", entry.Entity.Summary, StringComparison.OrdinalIgnoreCase);
    }
}
