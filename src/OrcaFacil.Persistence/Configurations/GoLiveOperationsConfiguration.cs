using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class GoLiveChecklistItemConfiguration : IEntityTypeConfiguration<GoLiveChecklistItem>
{
    public void Configure(EntityTypeBuilder<GoLiveChecklistItem> b) { b.ToTable("go_live_checklist_items", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.Code).HasMaxLength(80).IsRequired(); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.ResponsibleName).HasMaxLength(160); b.Property(x=>x.Observation).HasMaxLength(2000); b.HasIndex(x=>new{x.AccountId,x.Code}).IsUnique(); b.HasQueryFilter(x=>!x.IsDeleted); }
}
public sealed class GoLiveAccountStateConfiguration : IEntityTypeConfiguration<GoLiveAccountState>
{
    public void Configure(EntityTypeBuilder<GoLiveAccountState> b) { b.ToTable("go_live_account_states", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(32); b.HasIndex(x=>x.AccountId).IsUnique(); b.HasQueryFilter(x=>!x.IsDeleted); }
}
public sealed class TrainingProgressConfiguration : IEntityTypeConfiguration<TrainingProgress>
{
    public void Configure(EntityTypeBuilder<TrainingProgress> b) { b.ToTable("training_progress", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.LessonCode).HasMaxLength(80).IsRequired(); b.HasIndex(x=>new{x.AccountId,x.UserId,x.LessonCode}).IsUnique(); b.HasQueryFilter(x=>!x.IsDeleted); }
}
public sealed class CriticalRouteEventConfiguration : IEntityTypeConfiguration<CriticalRouteEvent>
{
    public void Configure(EntityTypeBuilder<CriticalRouteEvent> b) { b.ToTable("critical_route_events", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.Route).HasMaxLength(300).IsRequired(); b.Property(x=>x.CorrelationId).HasMaxLength(100).IsRequired(); b.Property(x=>x.ErrorFingerprint).HasMaxLength(32); b.Property(x=>x.SanitizedError).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.CreatedAt}); b.HasIndex(x=>new{x.Route,x.StatusCode,x.CreatedAt}); b.HasQueryFilter(x=>!x.IsDeleted); }
}
public sealed class AssistedOperationActionConfiguration : IEntityTypeConfiguration<AssistedOperationAction>
{
    public void Configure(EntityTypeBuilder<AssistedOperationAction> b) { b.ToTable("assisted_operation_actions", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.Notes).HasMaxLength(2000).IsRequired(); b.HasIndex(x=>new{x.AccountId,x.CompletedAt,x.DueAt}); b.HasQueryFilter(x=>!x.IsDeleted); }
}
