using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Plan).HasConversion<string>();
        builder.Property(x => x.TrialStatus).HasConversion<string>().HasMaxLength(30);
    }
}
