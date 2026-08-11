using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class DocumentRevisionConfiguration : IEntityTypeConfiguration<DocumentRevision>
{
    public void Configure(EntityTypeBuilder<DocumentRevision> b)
    {
        b.ToTable("document_revisions"); b.ConfigureBase();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.SnapshotHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.ProtectedSnapshot).HasColumnType("text").IsRequired();
        b.Property(x => x.BrandingSnapshot).HasColumnType("jsonb");
        b.Property(x => x.TemplateCode).HasMaxLength(40); b.Property(x => x.Total).HasPrecision(18, 2);
        b.Property(x => x.Version).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.DocumentId, x.VersionNumber }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.DocumentId, x.IsCurrent }).HasFilter("is_current = true").IsUnique();
        b.HasIndex(x => new { x.AccountId, x.Status, x.ValidUntil });
        b.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PublicDocumentAccessConfiguration : IEntityTypeConfiguration<PublicDocumentAccess>
{
    public void Configure(EntityTypeBuilder<PublicDocumentAccess> b)
    {
        b.ToTable("public_document_accesses"); b.ConfigureBase();
        b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Version).IsRowVersion(); b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.DocumentId, x.Status }); b.HasIndex(x => new { x.DocumentRevisionId, x.Status });
        b.HasOne<DocumentRevision>().WithMany().HasForeignKey(x => x.DocumentRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PublicDocumentDecisionConfiguration : IEntityTypeConfiguration<PublicDocumentDecision>
{
    public void Configure(EntityTypeBuilder<PublicDocumentDecision> b)
    {
        b.ToTable("public_document_decisions"); b.ConfigureBase(); b.Property(x => x.Decision).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.CustomerName).HasMaxLength(180); b.Property(x => x.ReasonCode).HasMaxLength(40); b.Property(x => x.Comment).HasMaxLength(1000);
        b.Property(x => x.IpHash).HasMaxLength(128); b.Property(x => x.UserAgentHash).HasMaxLength(128); b.Property(x => x.IdempotencyKey).HasMaxLength(128);
        b.HasIndex(x => new { x.AccountId, x.DocumentRevisionId }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.IdempotencyKey }).IsUnique();
    }
}

public sealed class CommercialFollowUpConfiguration : IEntityTypeConfiguration<CommercialFollowUp>
{
    public void Configure(EntityTypeBuilder<CommercialFollowUp> b)
    {
        b.ToTable("commercial_follow_ups"); b.ConfigureBase(); b.Property(x => x.Channel).HasConversion<string>(); b.Property(x => x.Result).HasConversion<string>();
        b.Property(x => x.Note).HasMaxLength(1000); b.HasIndex(x => new { x.AccountId, x.DocumentId, x.OccurredAt });
    }
}

public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> b)
    {
        b.ToTable("work_orders"); b.ConfigureBase(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); b.Property(x => x.Number).HasMaxLength(40);
        b.Property(x => x.Title).HasMaxLength(180); b.Property(x => x.Description).HasMaxLength(2000); b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.AddressSnapshot).HasColumnType("jsonb"); b.Property(x => x.ClientSnapshot).HasColumnType("jsonb"); b.Property(x => x.ItemsSnapshot).HasColumnType("jsonb");
        b.Property(x => x.TotalSnapshot).HasPrecision(18, 2); b.Property(x => x.PaymentMethod).HasMaxLength(80); b.Property(x => x.Version).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.Number }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.SourceRevisionId }).HasFilter("source_revision_id IS NOT NULL").IsUnique();
        b.HasIndex(x => new { x.AccountId, x.Status, x.ScheduledStart }); b.HasIndex(x => new { x.AccountId, x.AssignedUserId, x.ScheduledStart });
    }
}

public sealed class WorkOrderChecklistItemConfiguration : IEntityTypeConfiguration<WorkOrderChecklistItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderChecklistItem> b)
    {
        b.ToTable("work_order_checklist_items"); b.ConfigureBase();
        b.Property(x => x.Description).HasMaxLength(240).IsRequired();
        b.Property(x => x.CompletionNote).HasMaxLength(1000);
        b.HasIndex(x => new { x.AccountId, x.WorkOrderId, x.Position });
        b.HasOne<BusinessAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CompletedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ManualPaymentConfiguration : IEntityTypeConfiguration<ManualPayment>
{
    public void Configure(EntityTypeBuilder<ManualPayment> b)
    {
        b.ToTable("manual_payments"); b.ConfigureBase();
        b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.PaymentMethod).HasMaxLength(40);
        b.Property(x => x.Notes).HasMaxLength(1000); b.Property(x => x.IdempotencyKey).HasMaxLength(128);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.ReversalReason).HasMaxLength(500);
        b.HasIndex(x => new { x.AccountId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.WorkOrderId, x.PaidAt });
    }
}

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> b)
    {
        b.ToTable("receipts"); b.ConfigureBase(); b.Property(x => x.Number).HasMaxLength(40);
        b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.AmountInWords).HasMaxLength(500);
        b.Property(x => x.PaymentMethod).HasMaxLength(40); b.Property(x => x.City).HasMaxLength(180);
        b.Property(x => x.Notes).HasMaxLength(1000); b.Property(x => x.FiscalNotice).HasMaxLength(500);
        b.Property(x => x.IssuerSnapshot).HasColumnType("jsonb"); b.Property(x => x.ClientSnapshot).HasColumnType("jsonb");
        b.Property(x => x.ServiceSnapshot).HasColumnType("jsonb");
        b.Property(x => x.OriginType).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.ServiceDescription).HasMaxLength(1000).IsRequired();
        b.Property(x => x.CancellationReason).HasMaxLength(500); b.Property(x => x.PdfStorageKey).HasMaxLength(500);
        b.HasIndex(x => new { x.AccountId, x.Number }).IsUnique(); b.HasIndex(x => new { x.AccountId, x.PaymentId }).IsUnique();
    }
}
