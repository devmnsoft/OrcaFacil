using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class GrowthLeadConfiguration : IEntityTypeConfiguration<GrowthLead>
{
    public void Configure(EntityTypeBuilder<GrowthLead> builder)
    {
        builder.ToTable("growth_leads", "orcafacil"); builder.ConfigureBase();
        builder.Property(x => x.Name).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(254); builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Company).HasMaxLength(180); builder.Property(x => x.Role).HasMaxLength(100);
        builder.Property(x => x.Segment).HasMaxLength(100); builder.Property(x => x.CompanySize).HasMaxLength(40);
        builder.Property(x => x.Interest).HasMaxLength(80).IsRequired(); builder.Property(x => x.DesiredPlan).HasMaxLength(80);
        builder.Property(x => x.Message).HasMaxLength(1200); builder.Property(x => x.Source).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(80).IsRequired(); builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        foreach (var property in new[] { nameof(GrowthLead.UtmSource), nameof(GrowthLead.UtmMedium), nameof(GrowthLead.UtmCampaign), nameof(GrowthLead.UtmTerm), nameof(GrowthLead.UtmContent) }) builder.Property(property).HasMaxLength(200);
        builder.Property(x => x.ReferralCode).HasMaxLength(100); builder.Property(x => x.Gclid).HasMaxLength(300); builder.Property(x => x.Fbclid).HasMaxLength(300);
        builder.Property(x => x.LandingPage).HasMaxLength(500); builder.Property(x => x.Referrer).HasMaxLength(500);
        builder.HasIndex(x => new { x.TenantOwnerAccountId, x.Email }); builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
public sealed class GrowthLeadEventConfiguration : IEntityTypeConfiguration<GrowthLeadEvent>
{
    public void Configure(EntityTypeBuilder<GrowthLeadEvent> builder)
    {
        builder.ToTable("growth_lead_events", "orcafacil"); builder.ConfigureBase();
        builder.Property(x => x.EventType).HasMaxLength(60).IsRequired(); builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne<GrowthLead>().WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LeadId, x.OccurredAt });
    }
}
