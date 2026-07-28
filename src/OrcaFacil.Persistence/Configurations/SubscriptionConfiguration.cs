using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.Plan).HasConversion<string>();
        builder.Property(x => x.TrialStatus).HasConversion<string>().HasMaxLength(30);
        builder.HasOne<BusinessAccount>().WithMany().HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("subscriptions_account_id_fkey");
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_orcafacil_subscriptions_users");
        builder.HasOne<PlanVersion>().WithMany().HasForeignKey(x => x.SelectedPlanVersionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscriptions_selected_plan_version");
        builder.HasOne<PlanVersion>().WithMany().HasForeignKey(x => x.EffectivePlanVersionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_subscriptions_effective_plan_version");
    }
}
