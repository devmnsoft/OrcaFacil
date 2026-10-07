using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class BillingCoverageApplicationConfiguration : IEntityTypeConfiguration<BillingCoverageApplication>
{
    public void Configure(EntityTypeBuilder<BillingCoverageApplication> builder)
    {
        builder.ToTable("billing_coverage_applications");
        builder.ConfigureBase();
        builder.Property(x => x.ExternalPaymentId).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Effect).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.ExternalPaymentId, x.Effect }).IsUnique();
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasIndex(x => x.AccountId);
    }
}
