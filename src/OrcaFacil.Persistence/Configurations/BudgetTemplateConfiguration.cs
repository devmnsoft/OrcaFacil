using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class BudgetTemplateConfiguration : IEntityTypeConfiguration<BudgetTemplate>
{
    public void Configure(EntityTypeBuilder<BudgetTemplate> builder)
    {
        builder.ToTable("budget_templates", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Profession).HasColumnName("profession").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(800).IsRequired();
        builder.Property(x => x.IsSystemTemplate).HasColumnName("is_system_template").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.BudgetTemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Profession).HasDatabaseName("ix_orcafacil_budget_templates_profession");
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_orcafacil_budget_templates_user_id");
        builder.HasIndex(x => new { x.AccountId, x.IsActive }).HasDatabaseName("ix_budget_templates_account_active").HasFilter("is_deleted = false");
        builder.HasIndex(x => new { x.UserId, x.IsActive }).HasDatabaseName("ix_budget_templates_user_active").HasFilter("is_deleted = false");
        builder.HasIndex(x => new { x.IsSystemTemplate, x.IsActive }).HasDatabaseName("ix_budget_templates_system_active").HasFilter("is_deleted = false");
    }
}
