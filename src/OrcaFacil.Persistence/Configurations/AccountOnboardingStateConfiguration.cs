using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;
namespace OrcaFacil.Persistence.Configurations;
public sealed class AccountOnboardingStateConfiguration : IEntityTypeConfiguration<AccountOnboardingState>
{
 public void Configure(EntityTypeBuilder<AccountOnboardingState> b) { b.ToTable("account_onboarding_states"); b.ConfigureBase(); b.Property(x=>x.CurrentStep).HasConversion<string>().HasMaxLength(32); b.HasIndex(x=>new{x.AccountId,x.UserId}).IsUnique(); b.HasIndex(x=>new{x.CurrentStep,x.LastSeenAt}); }
}
