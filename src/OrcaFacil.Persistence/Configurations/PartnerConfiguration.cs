using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class PartnerConfiguration : IEntityTypeConfiguration<PartnerProfile>
{
    public void Configure(EntityTypeBuilder<PartnerProfile> b) { b.ToTable("partner_profiles"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(180).IsRequired(); b.Property(x => x.Category).HasConversion<string>().HasMaxLength(32); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.RatingAverage).HasPrecision(3, 2); b.HasIndex(x => new { x.AccountId, x.Name }); b.HasIndex(x => new { x.AccountId, x.SupplierId }).HasFilter("supplier_id IS NOT NULL").IsUnique(); }
}
public sealed class PartnerContactConfiguration : IEntityTypeConfiguration<PartnerContact>
{
    public void Configure(EntityTypeBuilder<PartnerContact> b) { b.ToTable("partner_contacts"); b.ConfigureBase(); b.Property(x => x.Email).HasMaxLength(254).IsRequired(); b.HasIndex(x => new { x.AccountId, x.PartnerId, x.Email }).IsUnique(); b.HasIndex(x => new { x.AccountId, x.PartnerId, x.IsPrimary }).HasFilter("is_primary=true AND is_deleted=false").IsUnique(); }
}
public sealed class PartnerPortalConfiguration : IEntityTypeConfiguration<PartnerPortalUser>
{
    public void Configure(EntityTypeBuilder<PartnerPortalUser> b) { b.ToTable("partner_portal_users"); b.ConfigureBase(); b.HasIndex(x => new { x.AccountId, x.PartnerId, x.EmailNormalized }).IsUnique(); }
}
public sealed class PartnerInvitationConfiguration : IEntityTypeConfiguration<PartnerPortalInvitation>
{
    public void Configure(EntityTypeBuilder<PartnerPortalInvitation> b) { b.ToTable("partner_portal_invitations"); b.ConfigureBase(); b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired(); b.HasIndex(x => x.TokenHash).IsUnique(); }
}
public sealed class OutsourcingConfiguration : IEntityTypeConfiguration<OutsourcingAssignment>
{
    public void Configure(EntityTypeBuilder<OutsourcingAssignment> b) { b.ToTable("outsourcing_assignments"); b.ConfigureBase(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.AgreedAmount).HasPrecision(18, 2); b.HasCheckConstraint("ck_outsourcing_assignment_amount", "agreed_amount >= 0"); b.HasIndex(x => new { x.AccountId, x.WorkOrderId }).HasFilter("is_deleted=false AND status NOT IN ('Canceled','Rejected')").IsUnique(); }
}
public sealed class PartnerModelsConfiguration : IEntityTypeConfiguration<OutsourcingQuote>
{
    public void Configure(EntityTypeBuilder<OutsourcingQuote> b)
    {
        b.ToTable("outsourcing_quotes"); b.ConfigureBase(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.TotalAmount).HasPrecision(18, 2); b.HasCheckConstraint("ck_outsourcing_quote_values", "total_amount >= 0 AND lead_time_days >= 0"); b.HasIndex(x => new { x.AccountId, x.OutsourcingRequestId, x.PartnerId }).IsUnique();
    }
}
public sealed class PartnerRemainingConfiguration : IEntityTypeConfiguration<PartnerCapability>
{
    public void Configure(EntityTypeBuilder<PartnerCapability> b)
    {
        b.ToTable("partner_capabilities"); b.ConfigureBase(); b.Property(x => x.DefaultCost).HasPrecision(18, 2); b.HasCheckConstraint("ck_partner_capability_values", "default_cost >= 0 AND default_lead_time_days >= 0");
        Map<PartnerServiceArea>(b.Metadata.Model, "partner_service_areas"); Map<PartnerDocument>(b.Metadata.Model, "partner_documents"); Map<PartnerPortalSession>(b.Metadata.Model, "partner_portal_sessions"); Map<PartnerPortalSecurityEvent>(b.Metadata.Model, "partner_portal_security_events"); Map<OutsourcingRequest>(b.Metadata.Model, "outsourcing_requests"); Map<OutsourcingRequestItem>(b.Metadata.Model, "outsourcing_request_items"); Map<OutsourcingQuoteItem>(b.Metadata.Model, "outsourcing_quote_items"); Map<PartnerWorkOrderUpdate>(b.Metadata.Model, "partner_work_order_updates"); Map<PartnerWorkOrderEvidence>(b.Metadata.Model, "partner_work_order_evidences"); Map<PartnerPaymentRequest>(b.Metadata.Model, "partner_payment_requests"); Map<PartnerCostSnapshot>(b.Metadata.Model, "partner_cost_snapshots"); Map<PartnerRating>(b.Metadata.Model, "partner_ratings"); Map<PartnerTermsAcceptance>(b.Metadata.Model, "partner_terms_acceptances");
    }
    private static void Map<T>(Microsoft.EntityFrameworkCore.Metadata.IMutableModel model, string table) where T : class => model.FindEntityType(typeof(T))!.SetTableName(table);
}
