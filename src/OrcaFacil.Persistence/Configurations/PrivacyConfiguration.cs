using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> b)
    {
        b.ToTable("legal_documents", "orcafacil"); b.ConfigureBase();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Code).HasMaxLength(80).IsRequired(); b.Property(x => x.Title).HasMaxLength(180).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}
public sealed class LegalDocumentVersionConfiguration : IEntityTypeConfiguration<LegalDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LegalDocumentVersion> b)
    {
        b.ToTable("legal_document_versions", "orcafacil"); b.ConfigureBase();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30); b.Property(x => x.VersionCode).HasMaxLength(40);
        b.Property(x => x.ContentHash).HasMaxLength(128); b.HasIndex(x => new { x.LegalDocumentId, x.VersionCode }).IsUnique();
        b.HasOne(x => x.LegalDocument).WithMany(x => x.Versions).HasForeignKey(x => x.LegalDocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class LegalAcceptanceConfiguration : IEntityTypeConfiguration<LegalAcceptance>
{
    public void Configure(EntityTypeBuilder<LegalAcceptance> b)
    {
        b.ToTable("legal_acceptances", "orcafacil"); b.ConfigureBase(); b.Property(x => x.AcceptanceSource).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.IpHash).HasMaxLength(128); b.Property(x => x.UserAgentHash).HasMaxLength(128);
        b.HasIndex(x => new { x.UserId, x.LegalDocumentVersionId }).IsUnique(); b.HasIndex(x => x.AccountId);
    }
}
public sealed class CommunicationConsentConfiguration : IEntityTypeConfiguration<CommunicationConsent>
{
    public void Configure(EntityTypeBuilder<CommunicationConsent> b)
    {
        b.ToTable("communication_consents", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(30); b.HasIndex(x => new { x.UserId, x.Channel, x.Purpose }).IsUnique();
    }
}
public sealed class DataSubjectRequestConfiguration : IEntityTypeConfiguration<DataSubjectRequest>
{
    public void Configure(EntityTypeBuilder<DataSubjectRequest> b)
    {
        b.ToTable("data_subject_requests", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40); b.Property(x => x.Description).HasMaxLength(4000); b.HasIndex(x => new { x.AccountId, x.RequestedAt });
    }
}
public sealed class PrivacyVendorConfiguration : IEntityTypeConfiguration<PrivacyVendor>
{
    public void Configure(EntityTypeBuilder<PrivacyVendor> b) { b.ToTable("privacy_vendors", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(180); b.HasIndex(x => new { x.IsActive, x.Name }); }
}
public sealed class PrivacyProcessingActivityConfiguration : IEntityTypeConfiguration<PrivacyProcessingActivity>
{
    public void Configure(EntityTypeBuilder<PrivacyProcessingActivity> b) { b.ToTable("privacy_processing_activities", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(180); b.Property(x => x.Status).HasMaxLength(30); }
}
