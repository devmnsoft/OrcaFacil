using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class UserUsageConfiguration : IEntityTypeConfiguration<UserUsage>
{
    public void Configure(EntityTypeBuilder<UserUsage> builder)
    {
        builder.ToTable("user_usage", "orcafacil");
        builder.ConfigureBase();
        builder.HasIndex(x => new { x.UserId, x.Period }).IsUnique();
    }
}
