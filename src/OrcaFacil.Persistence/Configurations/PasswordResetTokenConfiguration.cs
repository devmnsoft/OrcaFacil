using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;
namespace OrcaFacil.Persistence.Configurations;
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
 public void Configure(EntityTypeBuilder<PasswordResetToken> b) { b.ToTable("password_reset_tokens", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.TokenHash).HasMaxLength(64).IsRequired(); b.Property(x=>x.RequestedCorrelationId).HasMaxLength(100).IsRequired(); b.Property(x=>x.RequestedIpHash).HasMaxLength(64); b.Property(x=>x.UserAgentHash).HasMaxLength(64); b.Property(x=>x.CreatedBy).HasMaxLength(60).IsRequired(); b.HasIndex(x=>x.TokenHash).IsUnique(); b.HasIndex(x=>x.UserId); b.HasIndex(x=>x.ExpiresAt); b.HasIndex(x=>x.UsedAt); b.HasIndex(x=>x.RevokedAt); b.HasIndex(x=>x.CreatedAt); b.HasOne<UserAccount>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict); }
}
