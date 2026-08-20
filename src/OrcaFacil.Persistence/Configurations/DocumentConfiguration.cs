using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.HasIndex(x => new { x.AccountId, x.ClientId });
        builder.Property(x => x.InternalApprovalStatus).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(x => new { x.AccountId, x.BusinessUnitId, x.AssignedTeamId, x.AssignedToUserId });
        builder.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Number).HasColumnName("number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ClientName).HasColumnName("client_name").HasMaxLength(180).IsRequired();
        builder.Property(x => x.ClientDocument).HasColumnName("client_document").HasMaxLength(32);
        builder.Property(x => x.ClientPhone).HasColumnName("client_phone").HasMaxLength(40);
        builder.Property(x => x.ClientEmail).HasColumnName("client_email").HasMaxLength(254);
        builder.Property(x => x.ClientCity).HasColumnName("client_city").HasMaxLength(120);
        builder.Property(x => x.ClientSnapshot).HasColumnName("client_snapshot").HasColumnType("jsonb");
        builder.Property(x => x.IssueDate).HasColumnName("issue_date");
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until");
        builder.Property(x => x.EstimatedDuration).HasMaxLength(120);
        builder.Property(x => x.PaymentMethod).HasMaxLength(60);
        builder.Property(x => x.DepositAmount).HasPrecision(18, 2);
        builder.Property(x => x.PixInformation).HasMaxLength(300);
        builder.Property(x => x.WarrantyText).HasMaxLength(2000);
        builder.Property(x => x.ConditionsText).HasMaxLength(4000);
        builder.Property(x => x.TemplateCode).HasMaxLength(40);
        builder.Property(x => x.TemplateSnapshot).HasColumnType("jsonb");
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.Property(x => x.LastAutosaveKey).HasMaxLength(80);
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(4000);
        builder.Property(x => x.NextFollowUpAt).HasColumnName("next_follow_up_at");
        builder.Property(x => x.LastFollowUpAt).HasColumnName("last_follow_up_at");
        builder.Property(x => x.FollowUpStatus).HasColumnName("follow_up_status").HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.FollowUpNote).HasColumnName("follow_up_note").HasMaxLength(1000);
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.Discount).HasColumnName("discount").HasPrecision(18, 2);
        builder.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
        builder.Property(x => x.PublicToken).HasColumnName("public_token").HasMaxLength(128);
        builder.Property(x => x.PublicEnabled).HasColumnName("public_enabled");
        builder.Property(x => x.ClientDecision).HasColumnName("client_decision").HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.ClientDecisionAt).HasColumnName("client_decision_at");
        builder.Property(x => x.ClientDecisionNote).HasColumnName("client_decision_note").HasMaxLength(1000);
        builder.Property(x => x.EvidenceHash).HasColumnName("evidence_hash").HasMaxLength(128);
        builder.Property(x => x.OriginBudgetId).HasColumnName("origin_budget_id");
        builder.Property(x => x.OriginBudgetNumber).HasColumnName("origin_budget_number").HasMaxLength(40);
        builder.Property(x => x.ConvertedReceiptId).HasColumnName("converted_receipt_id");
        builder.Property(x => x.ConvertedReceiptNumber).HasColumnName("converted_receipt_number").HasMaxLength(40);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.Type, x.Number }).IsUnique();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.AccountId, x.Status, x.LastAutosavedAt });
        builder.HasIndex(x => new { x.AccountId, x.NextFollowUpAt });
    }
}
