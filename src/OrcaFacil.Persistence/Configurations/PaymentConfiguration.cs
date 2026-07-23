using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Plan).HasConversion<string>();
        builder.Property(x => x.Provider).HasMaxLength(40);
        builder.Property(x => x.PaymentMethod).HasMaxLength(80);
        builder.Property(x => x.ExternalPaymentId).HasMaxLength(180);
        builder.Property(x => x.ExternalReference).HasMaxLength(180);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(180);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Provider);
        builder.HasIndex(x => x.ExternalPaymentId);
        builder.HasIndex(x => x.ExternalReference);
        builder.HasIndex(x => x.DueDate);
    }
}
