using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class IssuerProfileConfiguration : IEntityTypeConfiguration<IssuerProfile>
{
    public void Configure(EntityTypeBuilder<IssuerProfile> builder)
    {
        builder.ToTable("issuer_profiles", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.BusinessName).HasColumnName("business_name").HasMaxLength(180).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
