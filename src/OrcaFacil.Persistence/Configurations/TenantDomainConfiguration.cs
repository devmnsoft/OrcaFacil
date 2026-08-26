using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("tenant_domains");
        builder.Property(x => x.HostName).HasMaxLength(253).IsRequired();
        builder.Property(x => x.NormalizedHostName).HasMaxLength(253).IsRequired();
        builder.Property(x => x.VerificationTokenHash).HasMaxLength(64);
        builder.Property(x => x.LastCheckStatus).HasMaxLength(80);
        builder.HasIndex(x => x.NormalizedHostName).IsUnique().HasFilter("is_deleted=false");
        builder.HasIndex(x => new { x.AccountId, x.Status });
    }
}

public sealed class TenantDomainVerificationConfiguration : IEntityTypeConfiguration<TenantDomainVerification>
{
    public void Configure(EntityTypeBuilder<TenantDomainVerification> builder)
    {
        builder.ToTable("tenant_domain_verifications");
        builder.Property(x => x.ResultCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.Property(x => x.ApprovalReason).HasMaxLength(1000);
        builder.HasIndex(x => new { x.AccountId, x.TenantDomainId, x.CreatedAt });
    }
}

public sealed class TenantDomainSslCheckConfiguration : IEntityTypeConfiguration<TenantDomainSslCheck>
{
    public void Configure(EntityTypeBuilder<TenantDomainSslCheck> builder)
    { builder.ToTable("tenant_domain_ssl_checks"); builder.Property(x => x.FailureReason).HasMaxLength(1000); }
}

public sealed class TenantEmailDomainConfiguration : IEntityTypeConfiguration<TenantEmailDomain>
{
    public void Configure(EntityTypeBuilder<TenantEmailDomain> builder)
    {
        builder.ToTable("tenant_email_domains"); builder.Property(x => x.DomainName).HasMaxLength(253).IsRequired();
        builder.HasIndex(x => x.DomainName).IsUnique().HasFilter("is_deleted=false");
    }
}

public sealed class TenantDomainAuditEventConfiguration : IEntityTypeConfiguration<TenantDomainAuditEvent>
{
    public void Configure(EntityTypeBuilder<TenantDomainAuditEvent> builder)
    {
        builder.ToTable("tenant_domain_audit_events"); builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000); builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasIndex(x => new { x.AccountId, x.CreatedAt });
    }
}
