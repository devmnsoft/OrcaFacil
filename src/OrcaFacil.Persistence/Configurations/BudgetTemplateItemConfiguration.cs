using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class BudgetTemplateItemConfiguration : IEntityTypeConfiguration<BudgetTemplateItem>
{
    public void Configure(EntityTypeBuilder<BudgetTemplateItem> builder)
    {
        builder.ToTable("budget_template_items", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.BudgetTemplateId).HasColumnName("budget_template_id").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(30).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.HasIndex(x => x.BudgetTemplateId).HasDatabaseName("ix_orcafacil_budget_template_items_template_id");
    }
}
