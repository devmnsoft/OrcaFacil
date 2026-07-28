using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class BusinessAccountConfiguration : IEntityTypeConfiguration<BusinessAccount> { public void Configure(EntityTypeBuilder<BusinessAccount> b) { b.ToTable("business_accounts"); b.ConfigureBase(); b.HasIndex(x => x.DocumentNumber).IsUnique(); b.HasIndex(x => x.Status); b.Property(x => x.Status).HasConversion<string>(); b.Property(x => x.PersonType).HasConversion<string>(); b.Property(x => x.DocumentType).HasConversion<string>(); b.Property(x => x.DocumentNumber).HasMaxLength(14).IsRequired(); b.Property(x => x.CurrentPlanCode).HasMaxLength(40); } }
public sealed class AccountMemberConfiguration : IEntityTypeConfiguration<AccountMember>
{
    public void Configure(EntityTypeBuilder<AccountMember> b)
    {
        b.ToTable("account_members");
        b.ConfigureBase();
        b.HasIndex(x => new { x.AccountId, x.UserId }).IsUnique();
        b.HasIndex(x => x.UserId);
        b.Property(x => x.Status).HasConversion<string>();
        b.HasOne<BusinessAccount>().WithMany().HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("account_members_account_id_fkey");
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("account_members_user_id_fkey");
    }
}
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role> { public void Configure(EntityTypeBuilder<Role> b) { b.ToTable("roles"); b.ConfigureBase(); b.HasIndex(x => x.Code).IsUnique(); } }
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission> { public void Configure(EntityTypeBuilder<Permission> b) { b.ToTable("permissions"); b.ConfigureBase(); b.HasIndex(x => x.Code).IsUnique(); } }
public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission> { public void Configure(EntityTypeBuilder<RolePermission> b) { b.ToTable("role_permissions"); b.ConfigureBase(); b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique(); } }
public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan> { public void Configure(EntityTypeBuilder<Plan> b) { b.ToTable("plans"); b.ConfigureBase(); b.HasIndex(x => x.Code).IsUnique(); } }
public sealed class PlanVersionConfiguration : IEntityTypeConfiguration<PlanVersion> { public void Configure(EntityTypeBuilder<PlanVersion> b) { b.ToTable("plan_versions"); b.ConfigureBase(); b.HasIndex(x => new { x.PlanId, x.VersionNumber }).IsUnique(); b.Property(x => x.Status).HasConversion<string>(); } }
public sealed class FeatureConfiguration : IEntityTypeConfiguration<Feature> { public void Configure(EntityTypeBuilder<Feature> b) { b.ToTable("features"); b.ConfigureBase(); b.HasIndex(x => x.Code).IsUnique(); b.Property(x => x.ValueType).HasConversion<string>(); } }
public sealed class PlanFeatureValueConfiguration : IEntityTypeConfiguration<PlanFeatureValue> { public void Configure(EntityTypeBuilder<PlanFeatureValue> b) { b.ToTable("plan_feature_values"); b.ConfigureBase(); b.HasIndex(x => new { x.PlanVersionId, x.FeatureId }).IsUnique(); } }
public sealed class BillingInvoiceConfiguration : IEntityTypeConfiguration<BillingInvoice> { public void Configure(EntityTypeBuilder<BillingInvoice> b) { b.ToTable("billing_invoices"); b.ConfigureBase(); b.HasIndex(x => x.AccountId); b.HasIndex(x => x.Status); b.HasIndex(x => x.ExternalReference).IsUnique(); b.HasIndex(x => x.DueAt); b.Property(x => x.Status).HasConversion<string>(); b.Property(x => x.Cycle).HasConversion<string>(); } }
public sealed class PlanOverrideConfiguration : IEntityTypeConfiguration<PlanOverride> { public void Configure(EntityTypeBuilder<PlanOverride> b) { b.ToTable("plan_overrides"); b.ConfigureBase(); b.HasIndex(x => x.AccountId); } }
public sealed class SubscriptionEventConfiguration : IEntityTypeConfiguration<SubscriptionEvent> { public void Configure(EntityTypeBuilder<SubscriptionEvent> b) { b.ToTable("subscription_events"); b.ConfigureBase(); b.HasIndex(x => x.AccountId); } }
public sealed class SupportAccessSessionConfiguration : IEntityTypeConfiguration<SupportAccessSession> { public void Configure(EntityTypeBuilder<SupportAccessSession> b) { b.ToTable("support_access_sessions"); b.ConfigureBase(); b.HasIndex(x => x.AccountId); } }
public sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent> { public void Configure(EntityTypeBuilder<ActivityEvent> b) { b.ToTable("activity_events"); b.ConfigureBase(); b.HasIndex(x => x.AccountId); b.HasIndex(x => x.CreatedAt); } }
