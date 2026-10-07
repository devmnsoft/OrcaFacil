using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class AiUsageLogConfiguration : IEntityTypeConfiguration<AiUsageLog>
{
    public void Configure(EntityTypeBuilder<AiUsageLog> builder)
    {
        builder.ToTable("ai_usage_logs", "orcafacil");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.OperationType).HasColumnName("operation_type").HasMaxLength(80);
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(80);
        builder.Property(x => x.Mode).HasColumnName("mode").HasMaxLength(32);
        builder.Property(x => x.EstimatedTokens).HasColumnName("estimated_tokens");
        builder.Property(x => x.EstimatedCost).HasColumnName("estimated_cost").HasPrecision(18, 6);
        builder.Property(x => x.DurationMs).HasColumnName("duration_ms");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
        builder.Property(x => x.SanitizedError).HasColumnName("sanitized_error");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => new { x.AccountId, x.CreatedAt });
    }
}

public sealed class AiSuggestionCardConfiguration : IEntityTypeConfiguration<AiSuggestionCard>
{
    public void Configure(EntityTypeBuilder<AiSuggestionCard> builder)
    {
        builder.ToTable("ai_suggestion_cards", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.DataJson).HasColumnName("data_json").HasColumnType("jsonb");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(24);
        builder.Property(x => x.AppliedDocumentId).HasColumnName("applied_document_id");
        builder.Property(x => x.ApplyFingerprint).HasColumnName("apply_fingerprint").HasMaxLength(128);
        builder.Property(x => x.AppliedAt).HasColumnName("applied_at");
        builder.HasIndex(x => new { x.AccountId, x.CreatedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
