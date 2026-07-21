using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("system_logs", "orcafacil");
        builder.ConfigureBase();
    }
}
