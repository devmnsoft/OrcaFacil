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
    }
}
