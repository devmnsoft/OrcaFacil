using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class PublicQuoteConfiguration : IEntityTypeConfiguration<PublicQuote>
{
    public void Configure(EntityTypeBuilder<PublicQuote> builder)
    {
        builder.ToTable("public_quotes", "public_access");
        builder.ConfigureBase();
        builder.Property(x => x.Token).HasColumnName("token").HasMaxLength(128).IsRequired();
        builder.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
        builder.Property(x => x.DocumentId).HasColumnName("document_id");
        builder.Property(x => x.PublicEnabled).HasColumnName("public_enabled");
        builder.Property(x => x.DecisionStatus).HasColumnName("decision_status").HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(x => x.Token).IsUnique();
    }
}
