using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class SystemErrorConfiguration : IEntityTypeConfiguration<SystemError>
{
    public void Configure(EntityTypeBuilder<SystemError> builder)
    {
        builder.ToTable("system_errors", "orcafacil");
        builder.ConfigureBase();
    }
}
