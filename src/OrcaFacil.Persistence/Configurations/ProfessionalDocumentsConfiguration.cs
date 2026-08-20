using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> b) { b.ToTable("file_assets"); b.ConfigureBase(); b.HasIndex(x => new { x.AccountId, x.CreatedAt }); b.HasIndex(x => new { x.AccountId, x.Sha256Hash }); b.Property(x=>x.OriginalFileName).HasMaxLength(255).IsRequired(); b.Property(x=>x.StoredFileName).HasMaxLength(80).IsRequired(); b.Property(x=>x.StoragePath).HasMaxLength(500).IsRequired(); b.Property(x=>x.ContentType).HasMaxLength(120).IsRequired(); b.Property(x=>x.Extension).HasMaxLength(12).IsRequired(); b.Property(x=>x.Sha256Hash).HasMaxLength(64).IsRequired(); }
}
public sealed class FileAssetLinkConfiguration : IEntityTypeConfiguration<FileAssetLink>
{
    public void Configure(EntityTypeBuilder<FileAssetLink> b) { b.ToTable("file_asset_links"); b.ConfigureBase(); b.HasIndex(x=>new{x.AccountId,x.EntityType,x.EntityId}); b.Property(x=>x.EntityType).HasMaxLength(40).IsRequired(); b.HasOne<FileAsset>().WithMany().HasForeignKey(x=>x.FileAssetId).OnDelete(DeleteBehavior.Restrict); }
}
public sealed class CompanyBrandingProfileConfiguration : IEntityTypeConfiguration<CompanyBrandingProfile>
{
    public void Configure(EntityTypeBuilder<CompanyBrandingProfile> b) { b.ToTable("company_branding_profiles"); b.ConfigureBase(); b.HasIndex(x=>x.AccountId).IsUnique(); b.Property(x=>x.TradeName).HasMaxLength(160).IsRequired(); b.Property(x=>x.PrimaryColor).HasMaxLength(7); b.Property(x=>x.SecondaryColor).HasMaxLength(7); b.HasOne<FileAsset>().WithMany().HasForeignKey(x=>x.LogoFileAssetId).OnDelete(DeleteBehavior.SetNull); }
}
public sealed class DocumentTemplateConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> b) { b.ToTable("document_templates"); b.ConfigureBase(); b.HasIndex(x=>new{x.AccountId,x.Type,x.IsDefault}); b.Property(x=>x.Name).HasMaxLength(160).IsRequired(); }
}
public sealed class DocumentTemplateVersionConfiguration : IEntityTypeConfiguration<DocumentTemplateVersion>
{
    public void Configure(EntityTypeBuilder<DocumentTemplateVersion> b) { b.ToTable("document_template_versions"); b.ConfigureBase(); b.HasIndex(x=>new{x.TemplateId,x.VersionNumber}).IsUnique(); b.Property(x=>x.VariablesJson).HasColumnType("jsonb"); b.HasOne<DocumentTemplate>().WithMany().HasForeignKey(x=>x.TemplateId).OnDelete(DeleteBehavior.Restrict); }
}
public sealed class DocumentAuditEventConfiguration : IEntityTypeConfiguration<DocumentAuditEvent>
{
    public void Configure(EntityTypeBuilder<DocumentAuditEvent> b) { b.ToTable("document_audit_events"); b.ConfigureBase(); b.HasIndex(x=>new{x.AccountId,x.CreatedAt}); b.Property(x=>x.EventType).HasMaxLength(80).IsRequired(); b.Property(x=>x.EntityType).HasMaxLength(40).IsRequired(); b.Property(x=>x.MetadataJson).HasColumnType("jsonb"); }
}
