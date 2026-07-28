using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;
namespace OrcaFacil.Persistence.Configurations;
public sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
 public void Configure(EntityTypeBuilder<EmailOutboxMessage> b) { b.ToTable("email_outbox_messages", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.TemplateCode).HasMaxLength(80).IsRequired(); b.Property(x=>x.RecipientHash).HasMaxLength(64).IsRequired(); b.Property(x=>x.RecipientMasked).HasMaxLength(254).IsRequired(); b.Property(x=>x.ProtectedRecipient).IsRequired(); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(20); b.Property(x=>x.Priority).HasConversion<string>().HasMaxLength(20); b.Property(x=>x.CorrelationId).HasMaxLength(100); b.Property(x=>x.IdempotencyKey).HasMaxLength(160).IsRequired(); b.Property(x=>x.ProcessingInstanceId).HasMaxLength(100); b.Property(x=>x.LastErrorCode).HasMaxLength(80); b.HasIndex(x=>x.IdempotencyKey).IsUnique(); b.HasIndex(x=>new{x.Status,x.NextAttemptAt,x.Priority}); b.HasIndex(x=>x.RecipientHash); }
}
