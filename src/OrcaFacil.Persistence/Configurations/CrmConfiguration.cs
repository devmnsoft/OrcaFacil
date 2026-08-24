using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class ClientRelationshipProfileConfiguration : IEntityTypeConfiguration<ClientRelationshipProfile>
{
    public void Configure(EntityTypeBuilder<ClientRelationshipProfile> b) { b.ToTable("client_relationship_profiles"); b.ConfigureBase(); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.StatusReason).HasMaxLength(500); b.Property(x=>x.CommercialTemperature).HasMaxLength(16); b.Property(x=>x.Source).HasMaxLength(120); b.HasIndex(x=>new{x.AccountId,x.ClientId}).IsUnique(); }
}
public sealed class ClientInteractionConfiguration : IEntityTypeConfiguration<ClientInteraction>
{
    public void Configure(EntityTypeBuilder<ClientInteraction> b) { b.ToTable("client_interactions"); b.ConfigureBase(); b.Property(x=>x.InteractionType).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.Description).HasMaxLength(5000).IsRequired(); b.Property(x=>x.Outcome).HasMaxLength(1000); b.HasIndex(x=>new{x.AccountId,x.ClientId,x.InteractionDate}); }
}
public sealed class ClientHealthScoreConfiguration : IEntityTypeConfiguration<ClientHealthScore>
{
    public void Configure(EntityTypeBuilder<ClientHealthScore> b) { b.ToTable("client_health_scores"); b.ConfigureBase(); b.Property(x=>x.Classification).HasMaxLength(32); b.Property(x=>x.ExplanationJson).HasColumnType("jsonb"); b.HasIndex(x=>new{x.AccountId,x.ClientId,x.CalculatedAt}); }
}
public sealed class CommunicationOptOutConfiguration : IEntityTypeConfiguration<CommunicationOptOut>
{
    public void Configure(EntityTypeBuilder<CommunicationOptOut> b) { b.ToTable("communication_opt_outs"); b.ConfigureBase(); b.Property(x=>x.Channel).HasConversion<string>().HasMaxLength(32); b.Property(x=>x.Reason).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.ClientId,x.Channel}).IsUnique(); }
}
public sealed class NpsResponseConfiguration : IEntityTypeConfiguration<NpsResponse>
{
    public void Configure(EntityTypeBuilder<NpsResponse> b) { b.ToTable("nps_responses"); b.ConfigureBase(); b.Property(x=>x.Comment).HasMaxLength(3000); b.HasCheckConstraint("ck_nps_score", "score >= 0 AND score <= 10"); b.HasIndex(x=>new{x.AccountId,x.SurveyId,x.ClientId}).IsUnique(); }
}
public sealed class RetentionRiskEventConfiguration : IEntityTypeConfiguration<RetentionRiskEvent>
{
    public void Configure(EntityTypeBuilder<RetentionRiskEvent> b) { b.ToTable("retention_risk_events"); b.ConfigureBase(); b.Property(x=>x.Level).HasConversion<string>().HasMaxLength(16); b.Property(x=>x.FactorCode).HasMaxLength(80); b.Property(x=>x.Reason).HasMaxLength(500); b.Property(x=>x.RecommendedAction).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.ClientId,x.FactorCode,x.ResolvedAt}); }
}
public sealed class CrmOpportunityConfiguration : IEntityTypeConfiguration<CrmOpportunity>
{
    public void Configure(EntityTypeBuilder<CrmOpportunity> b) { b.ToTable("crm_opportunities"); b.ConfigureBase(); b.Property(x=>x.Kind).HasMaxLength(16); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(16); b.Property(x=>x.Reason).HasMaxLength(1000); b.Property(x=>x.NextAction).HasMaxLength(500); b.Property(x=>x.DiscardReason).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.ClientId,x.Status}); }
}
