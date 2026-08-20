using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class BillingInvoiceItemConfiguration : IEntityTypeConfiguration<BillingInvoiceItem>
{
    public void Configure(EntityTypeBuilder<BillingInvoiceItem> b) { b.ToTable("billing_invoice_items"); b.ConfigureBase(); b.Property(x => x.Description).HasMaxLength(240).IsRequired(); b.Property(x => x.Quantity).HasPrecision(14, 3); b.Property(x => x.UnitAmount).HasPrecision(18, 2); b.Property(x => x.TotalAmount).HasPrecision(18, 2); b.HasIndex(x => x.InvoiceId); }
}
public sealed class BillingPaymentConfiguration : IEntityTypeConfiguration<BillingPayment>
{
    public void Configure(EntityTypeBuilder<BillingPayment> b) { b.ToTable("billing_payments"); b.ConfigureBase(); b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.PaymentMethod).HasConversion<string>(); b.Property(x => x.Status).HasConversion<string>(); b.Property(x => x.Reference).HasMaxLength(160); b.Property(x => x.ReversalReason).HasMaxLength(500); b.HasIndex(x => new { x.AccountId, x.InvoiceId }); }
}
public sealed class SubscriptionChangeRequestConfiguration : IEntityTypeConfiguration<SubscriptionChangeRequest>
{
    public void Configure(EntityTypeBuilder<SubscriptionChangeRequest> b) { b.ToTable("subscription_change_requests"); b.ConfigureBase(); b.Property(x => x.RequestType).HasConversion<string>(); b.Property(x => x.Status).HasConversion<string>(); b.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); b.Property(x => x.AdminNotes).HasMaxLength(1000); b.HasIndex(x => new { x.AccountId, x.Status }); }
}
public sealed class PlanAddonConfiguration : IEntityTypeConfiguration<PlanAddon>
{
    public void Configure(EntityTypeBuilder<PlanAddon> b) { b.ToTable("plan_addons"); b.ConfigureBase(); b.Property(x => x.Code).HasMaxLength(80).IsRequired(); b.HasIndex(x => x.Code).IsUnique(); b.Property(x => x.PriceMonthly).HasPrecision(18, 2); b.Property(x => x.PriceAnnual).HasPrecision(18, 2); }
}
public sealed class AccountAddonConfiguration : IEntityTypeConfiguration<AccountAddon>
{
    public void Configure(EntityTypeBuilder<AccountAddon> b) { b.ToTable("account_addons"); b.ConfigureBase(); b.HasIndex(x => new { x.AccountId, x.AddonId, x.DeactivatedAt }); }
}
public sealed class AccountEntitlementConfiguration : IEntityTypeConfiguration<AccountEntitlement>
{
    public void Configure(EntityTypeBuilder<AccountEntitlement> b) { b.ToTable("account_entitlements"); b.ConfigureBase(); b.Property(x => x.FeatureCode).HasMaxLength(100).IsRequired(); b.HasIndex(x => new { x.AccountId, x.FeatureCode }).IsUnique(); }
}
