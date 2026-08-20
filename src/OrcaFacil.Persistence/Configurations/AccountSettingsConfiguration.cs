using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class AccountSettingsConfiguration : IEntityTypeConfiguration<AccountSettings>
{
    public void Configure(EntityTypeBuilder<AccountSettings> builder)
    {
        builder.ToTable("account_settings", "orcafacil");
        builder.ConfigureBase();
        builder.HasIndex(x => x.AccountId).IsUnique();
        builder.Property(x => x.PrimaryColor).HasMaxLength(7);
        builder.Property(x => x.SecondaryColor).HasMaxLength(7);
        builder.Property(x => x.AccentColor).HasMaxLength(7);
        builder.Property(x => x.QuotePrefix).HasMaxLength(12).IsRequired();
        builder.Property(x => x.WorkOrderPrefix).HasMaxLength(12).IsRequired();
        builder.Property(x => x.ReceiptPrefix).HasMaxLength(12).IsRequired();
        builder.Property(x => x.NotificationPreferencesJson).HasColumnType("jsonb");
        builder.Property(x => x.CommunicationPreferencesJson).HasColumnType("jsonb");
        builder.HasOne<BusinessAccount>().WithOne().HasForeignKey<AccountSettings>(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_account_settings_business_account");
    }
}
