using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class DocumentItemConfiguration : IEntityTypeConfiguration<DocumentItem>
{
    public void Configure(EntityTypeBuilder<DocumentItem> builder)
    {
        builder.ToTable("document_items", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.DocumentId).HasColumnName("document_id");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(40).IsRequired();
        builder.Property(x => x.EstimatedCostSnapshot).HasColumnName("estimated_cost_snapshot").HasPrecision(18, 2);
        builder.Property(x => x.CategorySnapshot).HasColumnName("category_snapshot").HasMaxLength(80);
        builder.Property(x => x.DurationMinutesSnapshot).HasColumnName("duration_minutes_snapshot");
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasColumnName("discount").HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
        builder.HasIndex(x => x.DocumentId);
    }
}
