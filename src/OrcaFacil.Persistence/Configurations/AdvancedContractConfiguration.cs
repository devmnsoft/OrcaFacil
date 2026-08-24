using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class ContractSlaPolicyConfiguration : IEntityTypeConfiguration<ContractSlaPolicy>
{
    public void Configure(EntityTypeBuilder<ContractSlaPolicy> b) { b.ToTable("contract_sla_policies"); b.ConfigureBase(); b.Property(x=>x.Name).HasMaxLength(120).IsRequired(); b.Property(x=>x.Priority).HasConversion<string>().HasMaxLength(16); b.Property(x=>x.BusinessDaysJson).HasColumnType("jsonb"); b.HasIndex(x=>new{x.AccountId,x.ContractId,x.IsActive}); }
}
public sealed class ContractSlaEventConfiguration : IEntityTypeConfiguration<ContractSlaEvent>
{
    public void Configure(EntityTypeBuilder<ContractSlaEvent> b) { b.ToTable("contract_sla_events"); b.ConfigureBase(); b.Property(x=>x.EventType).HasMaxLength(48).IsRequired(); b.Property(x=>x.IdempotencyKey).HasMaxLength(180); b.Property(x=>x.Details).HasMaxLength(1000); b.HasIndex(x=>new{x.AccountId,x.IdempotencyKey}).HasFilter("idempotency_key IS NOT NULL").IsUnique(); b.HasIndex(x=>new{x.AccountId,x.ContractId,x.OccurredAt}); }
}
public sealed class ServiceLevelBreachConfiguration : IEntityTypeConfiguration<ServiceLevelBreach>
{
    public void Configure(EntityTypeBuilder<ServiceLevelBreach> b) { b.ToTable("service_level_breaches"); b.ConfigureBase(); b.Property(x=>x.BreachType).HasMaxLength(32); b.Property(x=>x.IdempotencyKey).HasMaxLength(180); b.HasIndex(x=>new{x.AccountId,x.IdempotencyKey}).IsUnique(); }
}
public sealed class ContractWarrantyTermConfiguration : IEntityTypeConfiguration<ContractWarrantyTerm>
{
    public void Configure(EntityTypeBuilder<ContractWarrantyTerm> b) { b.ToTable("contract_warranty_terms"); b.ConfigureBase(); b.Property(x=>x.Coverage).HasMaxLength(500).IsRequired(); b.Property(x=>x.Conditions).HasMaxLength(2000); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(20); b.Property(x=>x.CancellationReason).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.ContractId,x.Status,x.EndDate}); }
}
public sealed class ContractPreventiveScheduleConfiguration : IEntityTypeConfiguration<ContractPreventiveSchedule>
{
    public void Configure(EntityTypeBuilder<ContractPreventiveSchedule> b) { b.ToTable("contract_preventive_schedules"); b.ConfigureBase(); b.Property(x=>x.Name).HasMaxLength(160).IsRequired(); b.Property(x=>x.Description).HasMaxLength(2000).IsRequired(); b.Property(x=>x.Frequency).HasConversion<string>().HasMaxLength(24); b.HasIndex(x=>new{x.AccountId,x.IsActive,x.NextRunDate}); }
}
public sealed class ContractRecurrenceRunConfiguration : IEntityTypeConfiguration<ContractRecurrenceRun>
{
    public void Configure(EntityTypeBuilder<ContractRecurrenceRun> b) { b.ToTable("contract_recurrence_runs"); b.ConfigureBase(); b.Property(x=>x.RunType).HasMaxLength(32); b.Property(x=>x.IdempotencyKey).HasMaxLength(200); b.Property(x=>x.Status).HasMaxLength(24); b.Property(x=>x.ErrorSummary).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.IdempotencyKey}).IsUnique(); }
}
public sealed class ContractUsageAllowanceConfiguration : IEntityTypeConfiguration<ContractUsageAllowance>
{
    public void Configure(EntityTypeBuilder<ContractUsageAllowance> b) { b.ToTable("contract_usage_allowances"); b.ConfigureBase(); b.Property(x=>x.UsageType).HasMaxLength(40); b.Property(x=>x.Unit).HasMaxLength(30); b.Property(x=>x.AllowanceQuantity).HasPrecision(18,4); b.Property(x=>x.UsedQuantity).HasPrecision(18,4); b.HasIndex(x=>new{x.AccountId,x.ContractId,x.UsageType,x.PeriodStart}).IsUnique(); }
}
public sealed class ContractAmendmentConfiguration : IEntityTypeConfiguration<ContractAmendment>
{
    public void Configure(EntityTypeBuilder<ContractAmendment> b) { b.ToTable("contract_amendments"); b.ConfigureBase(); b.Property(x=>x.AmendmentNumber).HasMaxLength(40); b.Property(x=>x.Type).HasMaxLength(40); b.Property(x=>x.Description).HasMaxLength(2000); b.Property(x=>x.PreviousSnapshotJson).HasColumnType("jsonb"); b.Property(x=>x.NewSnapshotJson).HasColumnType("jsonb"); b.HasIndex(x=>new{x.AccountId,x.ContractId,x.AmendmentNumber}).IsUnique(); }
}
public sealed class ContractAdjustmentConfiguration : IEntityTypeConfiguration<ContractAdjustment>
{
    public void Configure(EntityTypeBuilder<ContractAdjustment> b) { b.ToTable("contract_adjustments"); b.ConfigureBase(); b.Property(x=>x.AdjustmentType).HasMaxLength(32); b.Property(x=>x.Reason).HasMaxLength(1000); b.Property(x=>x.Percent).HasPrecision(10,4); b.Property(x=>x.Amount).HasPrecision(18,2); b.Property(x=>x.OldValue).HasPrecision(18,2); b.Property(x=>x.NewValue).HasPrecision(18,2); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(24); b.HasIndex(x=>new{x.AccountId,x.ContractId,x.EffectiveDate}); }
}
public sealed class ContractRenewalEventConfiguration : IEntityTypeConfiguration<ContractRenewalEvent>
{
    public void Configure(EntityTypeBuilder<ContractRenewalEvent> b) { b.ToTable("contract_renewal_events"); b.ConfigureBase(); b.Property(x=>x.EventType).HasMaxLength(32); b.Property(x=>x.Reason).HasMaxLength(1000); b.Property(x=>x.IdempotencyKey).HasMaxLength(180); b.HasIndex(x=>new{x.AccountId,x.IdempotencyKey}).IsUnique(); }
}
public sealed class ContractHealthSnapshotConfiguration : IEntityTypeConfiguration<ContractHealthSnapshot>
{
    public void Configure(EntityTypeBuilder<ContractHealthSnapshot> b) { b.ToTable("contract_health_snapshots"); b.ConfigureBase(); b.Property(x=>x.Classification).HasMaxLength(30); b.Property(x=>x.PositiveFactorsJson).HasColumnType("jsonb"); b.Property(x=>x.RiskFactorsJson).HasColumnType("jsonb"); b.Property(x=>x.NextAction).HasMaxLength(500); b.HasIndex(x=>new{x.AccountId,x.ContractId,x.CalculatedAt}); }
}
