using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class DocumentItemConfiguration : IEntityTypeConfiguration<DocumentItem>
{
    public void Configure(EntityTypeBuilder<DocumentItem> builder)
    {
        builder.ToTable("document_items", "core");
        builder.ConfigureBase();
        builder.Property(x => x.DocumentId).HasColumnName("document_id");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasColumnName("discount").HasPrecision(18, 2);
        builder.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
        builder.HasIndex(x => x.DocumentId);
    }
}
