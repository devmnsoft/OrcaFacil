using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class ServiceCatalogItemConfiguration : IEntityTypeConfiguration<ServiceCatalogItem>
{
    public void Configure(EntityTypeBuilder<ServiceCatalogItem> builder)
    {
        builder.ToTable("service_catalog_items", "orcafacil"); builder.ConfigureBase();
        builder.Property(x => x.Code).HasMaxLength(40); builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1200); builder.Property(x => x.UnitCode).HasMaxLength(24).IsRequired();
        builder.Property(x => x.StandardPrice).HasPrecision(18, 2); builder.Property(x => x.EstimatedCost).HasPrecision(18, 2);
        builder.Property(x => x.DesiredMarginPercentage).HasPrecision(5, 2); builder.Property(x => x.DefaultDeliveryTerm).HasMaxLength(120);
        builder.Property(x => x.DefaultNotes).HasMaxLength(2000); builder.Property(x => x.Tags).HasMaxLength(500);
        builder.Property(x => x.InternalNotes).HasMaxLength(2000); builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.AccountId, x.Name }); builder.HasIndex(x => new { x.AccountId, x.Code }).IsUnique();
    }
}
public sealed class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder) { builder.ToTable("service_categories", "orcafacil"); builder.ConfigureBase(); builder.Property(x => x.Name).HasMaxLength(80).IsRequired(); builder.Property(x => x.NormalizedName).HasMaxLength(80).IsRequired(); builder.Property(x => x.Description).HasMaxLength(500); builder.Property(x => x.IconName).HasMaxLength(40); builder.HasIndex(x => new { x.AccountId, x.NormalizedName }).IsUnique(); }
}
public sealed class ServicePriceHistoryConfiguration : IEntityTypeConfiguration<ServicePriceHistory>
{
    public void Configure(EntityTypeBuilder<ServicePriceHistory> builder) { builder.ToTable("service_price_history", "orcafacil"); builder.ConfigureBase(); builder.Property(x => x.PreviousPrice).HasPrecision(18, 2); builder.Property(x => x.NewPrice).HasPrecision(18, 2); builder.Property(x => x.PreviousCost).HasPrecision(18, 2); builder.Property(x => x.NewCost).HasPrecision(18, 2); builder.Property(x => x.Reason).HasMaxLength(500); builder.HasIndex(x => new { x.AccountId, x.ServiceCatalogItemId, x.ChangedAt }); }
}
