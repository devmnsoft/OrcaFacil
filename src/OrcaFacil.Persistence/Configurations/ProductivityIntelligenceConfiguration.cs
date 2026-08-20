using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class RecommendationCardConfiguration : IEntityTypeConfiguration<RecommendationCard>
{
    public void Configure(EntityTypeBuilder<RecommendationCard> b)
    {
        b.ToTable("recommendation_cards", "orcafacil"); b.ConfigureBase();
        b.Property(x => x.Type).HasMaxLength(60).IsRequired(); b.Property(x => x.Priority).HasMaxLength(20).IsRequired();
        b.Property(x => x.Title).HasMaxLength(180).IsRequired(); b.Property(x => x.Description).HasMaxLength(800).IsRequired();
        b.Property(x => x.ActionUrl).HasMaxLength(400).IsRequired(); b.Property(x => x.Reason).HasMaxLength(800).IsRequired();
        b.HasIndex(x => new { x.AccountId, x.Status, x.Priority });
        b.HasIndex(x => new { x.AccountId, x.Type, x.DocumentId, x.WorkOrderId }).IsUnique().HasFilter("is_deleted = false AND status = 'Open'");
    }
}
public sealed class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> b) { b.ToTable("automation_rules", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.HasIndex(x => new { x.AccountId, x.IsActive }); }
}
public sealed class AutomationRunConfiguration : IEntityTypeConfiguration<AutomationRun>
{
    public void Configure(EntityTypeBuilder<AutomationRun> b) { b.ToTable("automation_runs", "orcafacil"); b.ConfigureBase(); b.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired(); b.HasIndex(x => new { x.AccountId, x.IdempotencyKey }).IsUnique(); }
}
public sealed class ProductivityEventConfiguration : IEntityTypeConfiguration<ProductivityEvent>
{
    public void Configure(EntityTypeBuilder<ProductivityEvent> b) { b.ToTable("productivity_events", "orcafacil"); b.ConfigureBase(); b.Property(x => x.EventType).HasMaxLength(60).IsRequired(); b.HasIndex(x => new { x.AccountId, x.OccurredAt }); }
}
