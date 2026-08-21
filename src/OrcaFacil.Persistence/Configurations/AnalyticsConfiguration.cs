using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class BusinessGoalConfiguration : IEntityTypeConfiguration<BusinessGoal>
{
    public void Configure(EntityTypeBuilder<BusinessGoal> b) { b.ToTable("business_goals"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(160).IsRequired(); b.Property(x => x.TargetValue).HasPrecision(18, 2); b.Property(x => x.CurrentValue).HasPrecision(18, 2); b.HasIndex(x => new { x.AccountId, x.StartDate, x.EndDate }); }
}
public sealed class GoalProgressSnapshotConfiguration : IEntityTypeConfiguration<GoalProgressSnapshot>
{
    public void Configure(EntityTypeBuilder<GoalProgressSnapshot> b) { b.ToTable("goal_progress_snapshots"); b.ConfigureBase(); b.HasIndex(x => new { x.AccountId, x.GoalId, x.ReferenceDate }).IsUnique(); b.Property(x => x.CurrentValue).HasPrecision(18, 2); b.Property(x => x.ProgressPercentage).HasPrecision(8, 2); }
}
public sealed class AnalyticsSnapshotConfiguration : IEntityTypeConfiguration<AnalyticsSnapshot>
{
    public void Configure(EntityTypeBuilder<AnalyticsSnapshot> b) { b.ToTable("analytics_snapshots"); b.ConfigureBase(); b.Property(x => x.Frequency).HasMaxLength(16); b.HasIndex(x => new { x.AccountId, x.Frequency, x.PeriodStart, x.PeriodEnd }).IsUnique(); }
}
public sealed class AnalyticsSnapshotItemConfiguration : IEntityTypeConfiguration<AnalyticsSnapshotItem>
{
    public void Configure(EntityTypeBuilder<AnalyticsSnapshotItem> b) { b.ToTable("analytics_snapshot_items"); b.ConfigureBase(); b.Property(x => x.MetricCode).HasMaxLength(80); b.Property(x => x.Value).HasPrecision(18, 2); b.HasIndex(x => new { x.AccountId, x.SnapshotId, x.MetricCode }).IsUnique(); }
}
public sealed class ForecastSnapshotConfiguration : IEntityTypeConfiguration<ForecastSnapshot>
{
    public void Configure(EntityTypeBuilder<ForecastSnapshot> b) { b.ToTable("forecast_snapshots"); b.ConfigureBase(); b.Property(x => x.ForecastType).HasMaxLength(32); b.Property(x => x.Confidence).HasMaxLength(24); b.Property(x => x.ForecastValue).HasPrecision(18, 2); b.HasIndex(x => new { x.AccountId, x.ForecastType, x.ReferenceDate, x.HorizonDays }).IsUnique(); }
}
public sealed class DataQualityFindingConfiguration : IEntityTypeConfiguration<DataQualityFinding>
{
    public void Configure(EntityTypeBuilder<DataQualityFinding> b) { b.ToTable("data_quality_findings"); b.ConfigureBase(); b.Property(x => x.RuleCode).HasMaxLength(80); b.Property(x => x.EntityType).HasMaxLength(40); b.Property(x => x.ActionUrl).HasMaxLength(500); b.HasIndex(x => new { x.AccountId, x.RuleCode, x.EntityType, x.EntityId }).IsUnique(); }
}
public sealed class DashboardWidgetPreferenceConfiguration : IEntityTypeConfiguration<DashboardWidgetPreference>
{
    public void Configure(EntityTypeBuilder<DashboardWidgetPreference> b) { b.ToTable("dashboard_widget_preferences"); b.ConfigureBase(); b.Property(x => x.WidgetCode).HasMaxLength(80); b.Property(x => x.FiltersJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.AccountId, x.UserId, x.WidgetCode }).IsUnique(); }
}
