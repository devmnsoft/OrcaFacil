using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents", "core");
        builder.ConfigureBase();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Number).HasColumnName("number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        builder.Property(x => x.ClientName).HasColumnName("client_name").HasMaxLength(180).IsRequired();
        builder.Property(x => x.ClientDocument).HasColumnName("client_document").HasMaxLength(32);
        builder.Property(x => x.ClientPhone).HasColumnName("client_phone").HasMaxLength(40);
        builder.Property(x => x.ClientEmail).HasColumnName("client_email").HasMaxLength(254);
        builder.Property(x => x.ClientCity).HasColumnName("client_city").HasMaxLength(120);
        builder.Property(x => x.IssueDate).HasColumnName("issue_date");
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until");
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(4000);
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
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.Type, x.Number }).IsUnique();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Status);
    }
}
