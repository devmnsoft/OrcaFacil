using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b) { b.ToTable("suppliers"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(180).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.HasIndex(x => new { x.AccountId, x.Name }); b.HasIndex(x => new { x.AccountId, x.DocumentNumber }).HasFilter("document_number IS NOT NULL").IsUnique(); }
}
public sealed class SupplyChainConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> b) { b.ToTable("materials"); b.ConfigureBase(); b.Property(x => x.Code).HasMaxLength(50).IsRequired(); b.Property(x => x.Name).HasMaxLength(180).IsRequired(); b.Property(x => x.DefaultCost).HasPrecision(18, 4); b.Property(x => x.DefaultSalePrice).HasPrecision(18, 2); b.Property(x => x.MinimumStock).HasPrecision(18, 4); b.HasCheckConstraint("ck_material_values", "default_cost >= 0 AND default_sale_price >= 0 AND minimum_stock >= 0"); b.HasIndex(x => new { x.AccountId, x.Code }).IsUnique(); }
}
public sealed class SupplyChainAuxiliaryConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b) { b.ToTable("inventory_items"); b.ConfigureBase(); b.Ignore(x => x.QuantityAvailable); b.Property(x => x.QuantityOnHand).HasPrecision(18, 4); b.Property(x => x.QuantityReserved).HasPrecision(18, 4); b.Property(x => x.AverageCost).HasPrecision(18, 4); b.HasCheckConstraint("ck_inventory_reserved", "quantity_reserved >= 0 AND quantity_reserved <= quantity_on_hand"); b.HasIndex(x => new { x.AccountId, x.MaterialId, x.InventoryLocationId }).IsUnique(); }
}
public sealed class SupplyChainModelConfiguration : IEntityTypeConfiguration<InventoryStockMovement>
{
    public void Configure(EntityTypeBuilder<InventoryStockMovement> b) { b.ToTable("inventory_stock_movements"); b.ConfigureBase(); b.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(32); b.Property(x => x.Quantity).HasPrecision(18, 4); b.HasCheckConstraint("ck_stock_movement_quantity", "quantity > 0"); b.HasIndex(x => new { x.AccountId, x.IdempotencyKey }).HasFilter("idempotency_key IS NOT NULL").IsUnique(); }
}
public sealed class SupplyChainRemainingConfiguration : IEntityTypeConfiguration<MarginPolicy>
{
    public void Configure(EntityTypeBuilder<MarginPolicy> b)
    {
        b.ToTable("margin_policies"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(120).IsRequired(); b.Property(x => x.MinimumMarginPercent).HasPrecision(7, 4); b.Property(x => x.WarningMarginPercent).HasPrecision(7, 4); b.HasIndex(x => new { x.AccountId, x.BusinessUnitId, x.IsActive });
    }
}
