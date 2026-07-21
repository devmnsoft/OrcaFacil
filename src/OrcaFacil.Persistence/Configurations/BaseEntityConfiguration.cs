using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Common;

namespace OrcaFacil.Persistence.Configurations;

internal static class BaseEntityConfiguration
{
    public static void ConfigureBase<TEntity>(this EntityTypeBuilder<TEntity> builder) where TEntity : Entity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
    }
}
