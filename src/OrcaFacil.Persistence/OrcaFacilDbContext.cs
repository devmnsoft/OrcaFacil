using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence;

public class OrcaFacilDbContext : DbContext
{
    public OrcaFacilDbContext(DbContextOptions<OrcaFacilDbContext> options) : base(options) { }

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<IssuerProfile> IssuerProfiles => Set<IssuerProfile>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentItem> DocumentItems => Set<DocumentItem>();
    public DbSet<PublicQuote> PublicQuotes => Set<PublicQuote>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<SystemError> SystemErrors => Set<SystemError>();
    public DbSet<UserUsage> UserUsage => Set<UserUsage>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AdminSetting> AdminSettings => Set<AdminSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orcafacil");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrcaFacilDbContext).Assembly);
    }
}
