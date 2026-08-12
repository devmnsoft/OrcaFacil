using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class CommercialLeadConfiguration : IEntityTypeConfiguration<CommercialLead>
{
    public void Configure(EntityTypeBuilder<CommercialLead> builder)
    {
        builder.ToTable("commercial_leads", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.Name).HasMaxLength(140).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(180);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Segment).HasMaxLength(100);
        builder.Property(x => x.Message).HasMaxLength(1200);
        builder.Property(x => x.SourcePage).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.InternalNotes).HasMaxLength(3000);
        builder.Property(x => x.DiscardReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.Email);
        builder.HasOne<BusinessAccount>().WithMany().HasForeignKey(x => x.ConvertedAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>().WithMany().HasForeignKey(x => x.ConvertedClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
