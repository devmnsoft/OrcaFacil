using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class BillingCustomerProfileConfiguration : IEntityTypeConfiguration<BillingCustomerProfile>
{
    public void Configure(EntityTypeBuilder<BillingCustomerProfile> builder)
    {
        builder.ToTable("billing_customer_profiles", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.PersonType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.DocumentNumber).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.AccountId).IsUnique();
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.HasOne<BusinessAccount>().WithOne().HasForeignKey<BillingCustomerProfile>(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("billing_customer_profiles_account_id_fkey");
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_orcafacil_billing_customer_profiles_users");
    }
}
