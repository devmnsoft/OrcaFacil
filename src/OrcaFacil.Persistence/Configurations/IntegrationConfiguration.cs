using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class IntegrationSettingConfiguration : IEntityTypeConfiguration<IntegrationSetting>
{
    public void Configure(EntityTypeBuilder<IntegrationSetting> b) { b.ToTable("integration_settings", "orcafacil"); b.ConfigureBase(); b.HasIndex(x => x.AccountId).IsUnique(); b.Property(x => x.PublicBaseUrl).HasMaxLength(500); b.Property(x => x.WhatsAppNumber).HasMaxLength(30); b.Property(x => x.SmtpHost).HasMaxLength(255); b.Property(x => x.SmtpUser).HasMaxLength(255); }
}
public sealed class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> b) { b.ToTable("webhook_endpoints", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(120).IsRequired(); b.Property(x => x.Url).HasMaxLength(1000).IsRequired(); b.Property(x => x.SecretHash).HasMaxLength(64).IsRequired(); b.Property(x => x.ProtectedSecret).IsRequired(); b.HasIndex(x => new { x.AccountId, x.IsActive }); }
}
public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> b) { b.ToTable("webhook_deliveries", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); b.Property(x => x.EventType).HasMaxLength(80).IsRequired(); b.Property(x => x.IdempotencyKey).HasMaxLength(180).IsRequired(); b.HasIndex(x => x.IdempotencyKey).IsUnique(); b.HasIndex(x => new { x.AccountId, x.Status, x.NextAttemptAt }); }
}
public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b) { b.ToTable("api_keys", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Name).HasMaxLength(120).IsRequired(); b.Property(x => x.KeyHash).HasMaxLength(64).IsRequired(); b.Property(x => x.Prefix).HasMaxLength(20).IsRequired(); b.Property(x => x.Scopes).HasMaxLength(500).IsRequired(); b.HasIndex(x => x.KeyHash).IsUnique(); b.HasIndex(x => new { x.AccountId, x.RevokedAt }); }
}

public sealed class ApiRequestLogConfiguration : IEntityTypeConfiguration<ApiRequestLog>
{
    public void Configure(EntityTypeBuilder<ApiRequestLog> b) { b.ToTable("api_request_logs", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Route).HasMaxLength(300); b.Property(x => x.Method).HasMaxLength(10); b.Property(x => x.IpAddress).HasMaxLength(64); b.Property(x => x.UserAgent).HasMaxLength(300); b.Property(x => x.CorrelationId).HasMaxLength(100); b.Property(x => x.ErrorCode).HasMaxLength(60); b.HasIndex(x => new { x.AccountId, x.CreatedAt }); b.HasIndex(x => new { x.ApiKeyId, x.CreatedAt }); }
}

public sealed class ApiIdempotencyKeyConfiguration : IEntityTypeConfiguration<ApiIdempotencyKey>
{
    public void Configure(EntityTypeBuilder<ApiIdempotencyKey> b) { b.ToTable("api_idempotency_keys", "orcafacil"); b.ConfigureBase(); b.Property(x => x.KeyHash).HasMaxLength(64); b.Property(x => x.RequestHash).HasMaxLength(64); b.Property(x => x.ResponseJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.AccountId, x.ApiKeyId, x.KeyHash }).IsUnique(); b.HasIndex(x => x.ExpiresAt); }
}
public sealed class DataExportConfiguration : IEntityTypeConfiguration<DataExport>
{
    public void Configure(EntityTypeBuilder<DataExport> b) { b.ToTable("data_exports", "orcafacil"); b.ConfigureBase(); b.Property(x => x.DataType).HasMaxLength(40).IsRequired(); b.Property(x => x.Format).HasMaxLength(10).IsRequired(); b.HasIndex(x => new { x.AccountId, x.CompletedAt }); }
}
