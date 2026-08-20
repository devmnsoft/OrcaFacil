using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

internal static class PrivacyGovernanceConfiguration
{
    internal static void AccountIndex<T>(EntityTypeBuilder<T> b) where T : class => b.HasIndex("AccountId", "CreatedAt");
}

public sealed class PrivacyConsentConfiguration : IEntityTypeConfiguration<PrivacyConsent>
{
    public void Configure(EntityTypeBuilder<PrivacyConsent> b) { b.ToTable("privacy_consents", "orcafacil"); b.ConfigureBase();
        b.Property(x => x.ConsentType).HasConversion<string>().HasMaxLength(40); b.Property(x => x.Version).HasMaxLength(40);
        b.Property(x => x.IpAddress).HasMaxLength(64); b.Property(x => x.UserAgent).HasMaxLength(512);
        b.HasIndex(x => new { x.AccountId, x.UserId, x.ConsentType, x.Version }); }
}
public sealed class DataExportJobConfiguration : IEntityTypeConfiguration<DataExportJob>
{ public void Configure(EntityTypeBuilder<DataExportJob> b) { b.ToTable("data_export_jobs", "orcafacil"); b.ConfigureBase(); PrivacyGovernanceConfiguration.AccountIndex(b); } }
public sealed class DataRetentionPolicyConfiguration : IEntityTypeConfiguration<DataRetentionPolicy>
{ public void Configure(EntityTypeBuilder<DataRetentionPolicy> b) { b.ToTable("data_retention_policies", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Action).HasConversion<string>().HasMaxLength(30); b.HasIndex(x => new { x.AccountId, x.DataType }).IsUnique(); } }
public sealed class DataRetentionRunConfiguration : IEntityTypeConfiguration<DataRetentionRun>
{ public void Configure(EntityTypeBuilder<DataRetentionRun> b) { b.ToTable("data_retention_runs", "orcafacil"); b.ConfigureBase(); PrivacyGovernanceConfiguration.AccountIndex(b); } }
public sealed class SensitiveDataAccessLogConfiguration : IEntityTypeConfiguration<SensitiveDataAccessLog>
{ public void Configure(EntityTypeBuilder<SensitiveDataAccessLog> b) { b.ToTable("sensitive_data_access_logs", "orcafacil"); b.ConfigureBase(); PrivacyGovernanceConfiguration.AccountIndex(b); b.HasIndex(x => new { x.AccountId, x.EntityType, x.EntityId }); } }
public sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{ public void Configure(EntityTypeBuilder<SecurityEvent> b) { b.ToTable("security_events", "orcafacil"); b.ConfigureBase(); b.HasIndex(x => new { x.AccountId, x.CreatedAt }); } }
public sealed class SessionRecordConfiguration : IEntityTypeConfiguration<SessionRecord>
{ public void Configure(EntityTypeBuilder<SessionRecord> b) { b.ToTable("session_records", "orcafacil"); b.ConfigureBase(); b.Ignore(x => x.IsActive); b.Property(x => x.SessionHash).HasMaxLength(128); b.HasIndex(x => new { x.AccountId, x.UserId, x.RevokedAt }); } }
public sealed class PublicTokenAccessLogConfiguration : IEntityTypeConfiguration<PublicTokenAccessLog>
{ public void Configure(EntityTypeBuilder<PublicTokenAccessLog> b) { b.ToTable("public_token_access_logs", "orcafacil"); b.ConfigureBase(); b.HasIndex(x => new { x.AccountId, x.AccessedAt }); } }
public sealed class AccountSecuritySettingConfiguration : IEntityTypeConfiguration<AccountSecuritySetting>
{ public void Configure(EntityTypeBuilder<AccountSecuritySetting> b) { b.ToTable("account_security_settings", "orcafacil"); b.ConfigureBase(); b.HasIndex(x => x.AccountId).IsUnique(); } }
public sealed class AuditExportJobConfiguration : IEntityTypeConfiguration<AuditExportJob>
{ public void Configure(EntityTypeBuilder<AuditExportJob> b) { b.ToTable("audit_export_jobs", "orcafacil"); b.ConfigureBase(); PrivacyGovernanceConfiguration.AccountIndex(b); } }
