using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;
namespace OrcaFacil.Persistence.Configurations;
public sealed class AccountInvitationConfiguration : IEntityTypeConfiguration<AccountInvitation>
{
 public void Configure(EntityTypeBuilder<AccountInvitation> b) { b.ToTable("account_invitations", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.Email).HasMaxLength(254).IsRequired(); b.Property(x=>x.FullName).HasMaxLength(180).IsRequired(); b.Property(x=>x.RoleCode).HasMaxLength(40).IsRequired(); b.Property(x=>x.TokenHash).HasMaxLength(64).IsRequired(); b.Property(x=>x.RequestedCorrelationId).HasMaxLength(100).IsRequired(); b.HasIndex(x=>x.TokenHash).IsUnique(); b.HasIndex(x=>x.AccountId); b.HasIndex(x=>new{x.AccountId,x.Email}); b.HasIndex(x=>x.ExpiresAt); b.HasIndex(x=>x.UsedAt); b.HasIndex(x=>x.CreatedAt); b.HasOne<BusinessAccount>().WithMany().HasForeignKey(x=>x.AccountId).OnDelete(DeleteBehavior.Restrict); b.HasOne<UserAccount>().WithMany().HasForeignKey(x=>x.InvitedByUserId).OnDelete(DeleteBehavior.Restrict); }
}
