using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class SaasModuleConfiguration : IEntityTypeConfiguration<SaasModule>
{
    public void Configure(EntityTypeBuilder<SaasModule> b)
    {
        b.ToTable("saas_modules"); b.ConfigureBase();
        b.Property(x => x.Code).HasMaxLength(80).IsRequired(); b.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000); b.Property(x => x.Category).HasMaxLength(80);
        b.Property(x => x.IconKey).HasMaxLength(80); b.Property(x => x.MenuGroup).HasMaxLength(80);
        b.Property(x => x.RoutePrefix).HasMaxLength(160); b.Property(x => x.RequiredPermissionCode).HasMaxLength(160);
        b.Property(x => x.BaseMonthlyPrice).HasPrecision(18, 2); b.Property(x => x.BaseAnnualPrice).HasPrecision(18, 2);
        b.HasIndex(x => x.Code).IsUnique(); b.HasIndex(x => new { x.IsActive, x.DisplayOrder });
    }
}

public sealed class SaasModuleFeatureConfiguration : IEntityTypeConfiguration<SaasModuleFeature>
{ public void Configure(EntityTypeBuilder<SaasModuleFeature> b) { b.ToTable("saas_module_features"); b.ConfigureBase(); b.Property(x=>x.Code).HasMaxLength(120).IsRequired(); b.Property(x=>x.DisplayName).HasMaxLength(160).IsRequired(); b.Property(x=>x.RequiredPermissionCode).HasMaxLength(160); b.HasIndex(x=>new{x.ModuleId,x.Code}).IsUnique(); } }
public sealed class SaasModulePriceConfiguration : IEntityTypeConfiguration<SaasModulePrice>
{ public void Configure(EntityTypeBuilder<SaasModulePrice> b) { b.ToTable("saas_module_prices"); b.ConfigureBase(); b.Property(x=>x.BillingPeriod).HasConversion<string>().HasMaxLength(16); b.Property(x=>x.Amount).HasPrecision(18,2); b.Property(x=>x.Currency).HasMaxLength(3); b.HasIndex(x=>new{x.ModuleId,x.BillingPeriod,x.ValidFrom}); } }
public sealed class AccountModuleSubscriptionConfiguration : IEntityTypeConfiguration<AccountModuleSubscription>
{ public void Configure(EntityTypeBuilder<AccountModuleSubscription> b) { b.ToTable("account_module_subscriptions"); b.ConfigureBase(); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(32); b.Property(x=>x.BillingPeriod).HasConversion<string>().HasMaxLength(16); b.Property(x=>x.ContractedPrice).HasPrecision(18,2); b.Property(x=>x.DiscountAmount).HasPrecision(18,2); b.Property(x=>x.Currency).HasMaxLength(3); b.Property(x=>x.ManualGrantReason).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.ModuleId}).IsUnique().HasFilter("is_deleted=false"); b.HasIndex(x=>new{x.AccountId,x.Status}); } }
public sealed class AccountModuleEntitlementConfiguration : IEntityTypeConfiguration<AccountModuleEntitlement>
{ public void Configure(EntityTypeBuilder<AccountModuleEntitlement> b) { b.ToTable("account_module_entitlements"); b.ConfigureBase(); b.Property(x=>x.FeatureCode).HasMaxLength(120); b.Property(x=>x.Source).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Reason).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.ModuleId,x.FeatureCode}).IsUnique().HasFilter("is_deleted=false"); } }
public sealed class AccountModuleFeatureLimitConfiguration : IEntityTypeConfiguration<AccountModuleFeatureLimit>
{ public void Configure(EntityTypeBuilder<AccountModuleFeatureLimit> b) { b.ToTable("account_module_feature_limits"); b.ConfigureBase(); b.Property(x=>x.FeatureCode).HasMaxLength(120); b.Property(x=>x.Period).HasMaxLength(24); b.HasIndex(x=>new{x.AccountId,x.ModuleId,x.FeatureCode,x.Period}).IsUnique().HasFilter("is_deleted=false"); } }
public sealed class AccountModuleUsageEventConfiguration : IEntityTypeConfiguration<AccountModuleUsageEvent>
{ public void Configure(EntityTypeBuilder<AccountModuleUsageEvent> b) { b.ToTable("account_module_usage_events"); b.ConfigureBase(); b.Property(x=>x.ModuleCode).HasMaxLength(80); b.Property(x=>x.FeatureCode).HasMaxLength(120); b.Property(x=>x.EventCode).HasMaxLength(120); b.Property(x=>x.Route).HasMaxLength(300); b.Property(x=>x.CorrelationId).HasMaxLength(100); b.HasIndex(x=>new{x.AccountId,x.ModuleCode,x.OccurredAt}); b.HasIndex(x=>new{x.AccountId,x.UserId,x.OccurredAt}); } }
public sealed class AccountModuleUsageSnapshotConfiguration : IEntityTypeConfiguration<AccountModuleUsageSnapshot>
{ public void Configure(EntityTypeBuilder<AccountModuleUsageSnapshot> b) { b.ToTable("account_module_usage_snapshots"); b.ConfigureBase(); b.Property(x=>x.ModuleCode).HasMaxLength(80); b.HasIndex(x=>new{x.AccountId,x.ModuleCode,x.PeriodStart}).IsUnique().HasFilter("is_deleted=false"); } }
public sealed class AccountModuleAuditLogConfiguration : IEntityTypeConfiguration<AccountModuleAuditLog>
{ public void Configure(EntityTypeBuilder<AccountModuleAuditLog> b) { b.ToTable("account_module_audit_logs"); b.ConfigureBase(); b.Property(x=>x.Action).HasMaxLength(120); b.Property(x=>x.EntityType).HasMaxLength(120); b.Property(x=>x.EntityId).HasMaxLength(100); b.Property(x=>x.ModuleCode).HasMaxLength(80); b.Property(x=>x.Summary).HasMaxLength(1000); b.Property(x=>x.CorrelationId).HasMaxLength(100); b.HasIndex(x=>new{x.AccountId,x.CreatedAt}); b.HasIndex(x=>new{x.Action,x.CreatedAt}); } }
public sealed class AccountMemberProfileConfiguration : IEntityTypeConfiguration<AccountMemberProfile>
{ public void Configure(EntityTypeBuilder<AccountMemberProfile> b) { b.ToTable("account_member_profiles"); b.ConfigureBase(); b.HasIndex(x=>new{x.AccountId,x.AccountMemberId,x.RoleId}).IsUnique().HasFilter("is_deleted=false"); } }
