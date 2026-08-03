using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("users", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Plan).HasColumnName("plan").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(40);
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.IsBlocked).HasColumnName("is_blocked");
        builder.Property(x => x.BlockReason).HasColumnName("block_reason").HasMaxLength(500);
        builder.Property(x => x.AcceptedTermsAt).HasColumnName("accepted_terms_at");
        builder.Property(x => x.AcceptedPrivacyAt).HasColumnName("accepted_privacy_at");
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at");
        builder.Property(x => x.SessionVersion).HasColumnName("session_version").HasDefaultValue(1);
        builder.Property(x => x.PasswordResetReason).HasMaxLength(500);
        builder.HasCheckConstraint("ck_users_role", "role IN ('User','Admin','SuperAdmin')");
        builder.HasCheckConstraint("ck_users_plan", "plan IN ('Free','Professional','Business')");
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
