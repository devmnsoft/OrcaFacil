using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.PersonType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.DocumentNumber).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.TradeName).HasMaxLength(180);
        builder.Property(x => x.LegalName).HasMaxLength(180);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.City).HasMaxLength(120);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.DocumentNumber);
    }
}
