using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class AdminSettingConfiguration : IEntityTypeConfiguration<AdminSetting>
{
    public void Configure(EntityTypeBuilder<AdminSetting> builder)
    {
        builder.ToTable("admin_settings", "admin");
        builder.ConfigureBase();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}
