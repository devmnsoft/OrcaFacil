using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class ServicePriceTableConfiguration : IEntityTypeConfiguration<ServicePriceTable>
{
    public void Configure(EntityTypeBuilder<ServicePriceTable> b) { b.ToTable("service_price_tables", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.Property(x => x.Description).HasMaxLength(1000); b.Property(x => x.Scope).HasConversion<string>().HasMaxLength(32); b.HasIndex(x => new { x.AccountId, x.IsActive, x.ValidFrom }); }
}
public sealed class ServicePriceTableItemConfiguration : IEntityTypeConfiguration<ServicePriceTableItem>
{
    public void Configure(EntityTypeBuilder<ServicePriceTableItem> b) { b.ToTable("service_price_table_items", "orcafacil"); b.ConfigureBase(); foreach (var p in new[] { nameof(ServicePriceTableItem.BasePrice), nameof(ServicePriceTableItem.MinimumPrice), nameof(ServicePriceTableItem.MaximumPrice), nameof(ServicePriceTableItem.MinimumMarginPercentage), nameof(ServicePriceTableItem.MaximumDiscountPercentage) }) b.Property(p).HasPrecision(18, 2); b.HasIndex(x => new { x.AccountId, x.ServicePriceTableId, x.ServiceCatalogItemId }).IsUnique(); }
}
public sealed class PricingMarginPolicyConfiguration : IEntityTypeConfiguration<PricingMarginPolicy>
{
    public void Configure(EntityTypeBuilder<PricingMarginPolicy> b) { b.ToTable("pricing_margin_policies", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.Property(x => x.MinimumMarginPercentage).HasPrecision(8, 2); b.Property(x => x.TargetMarginPercentage).HasPrecision(8, 2); b.HasIndex(x => new { x.AccountId, x.IsActive }); }
}
public sealed class PricingDiscountPolicyConfiguration : IEntityTypeConfiguration<PricingDiscountPolicy>
{
    public void Configure(EntityTypeBuilder<PricingDiscountPolicy> b) { b.ToTable("pricing_discount_policies", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.Property(x => x.MaximumPercentageWithoutApproval).HasPrecision(8, 2); b.Property(x => x.MaximumAmountWithoutApproval).HasPrecision(18, 2); b.HasIndex(x => new { x.AccountId, x.IsActive }); }
}
public sealed class PricingQuoteSnapshotConfiguration : IEntityTypeConfiguration<PricingQuoteSnapshot>
{
    public void Configure(EntityTypeBuilder<PricingQuoteSnapshot> b) { b.ToTable("pricing_quote_snapshots", "orcafacil"); b.ConfigureBase(); b.Property(x => x.PayloadJson).HasColumnType("jsonb"); b.Property(x => x.BasePrice).HasPrecision(18, 2); b.Property(x => x.Discount).HasPrecision(18, 2); b.Property(x => x.TotalCost).HasPrecision(18, 2); b.Property(x => x.TotalPrice).HasPrecision(18, 2); b.Property(x => x.MarginPercentage).HasPrecision(8, 2); b.HasIndex(x => new { x.AccountId, x.QuoteId, x.Sequence }).IsUnique(); }
}
public sealed class PricingApprovalEventConfiguration : IEntityTypeConfiguration<PricingApprovalEvent>
{
    public void Configure(EntityTypeBuilder<PricingApprovalEvent> b) { b.ToTable("pricing_approval_events", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Trigger).HasMaxLength(80).IsRequired(); b.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); b.Property(x => x.DecisionReason).HasMaxLength(1000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.HasIndex(x => new { x.AccountId, x.Status, x.CreatedAt }); }
}
