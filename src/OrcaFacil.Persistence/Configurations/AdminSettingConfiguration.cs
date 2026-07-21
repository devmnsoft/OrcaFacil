using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class AdminSettingConfiguration : IEntityTypeConfiguration<AdminSetting>
{
    public void Configure(EntityTypeBuilder<AdminSetting> builder)
    {
        builder.ToTable("admin_settings", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.ValueJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.Key).IsUnique();
    }
}
