using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class DataImportConfiguration : IEntityTypeConfiguration<DataImport>
{
    public void Configure(EntityTypeBuilder<DataImport> builder)
    {
        builder.ToTable("data_imports", "orcafacil"); builder.ConfigureBase();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.StagedRowsJson).HasColumnType("jsonb"); builder.Property(x => x.ErrorsJson).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.AccountId, x.CreatedAt });
    }
}
