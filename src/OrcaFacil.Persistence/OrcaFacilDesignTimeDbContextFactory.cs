using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrcaFacil.Persistence;

public sealed class OrcaFacilDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrcaFacilDbContext>
{
    public OrcaFacilDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = "Host=localhost;Port=5432;Database=orcafacil_design;Username=orcafacil_design";
        var options = new DbContextOptionsBuilder<OrcaFacilDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "orcafacil"))
            .EnableSensitiveDataLogging(false)
            .Options;
        return new OrcaFacilDbContext(options);
    }
}
