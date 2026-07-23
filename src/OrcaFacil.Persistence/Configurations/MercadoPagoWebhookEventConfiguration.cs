using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders; using OrcaFacil.Domain.Entities;
namespace OrcaFacil.Persistence.Configurations;
public class MercadoPagoWebhookEventConfiguration:IEntityTypeConfiguration<MercadoPagoWebhookEvent>{public void Configure(EntityTypeBuilder<MercadoPagoWebhookEvent>b){b.ToTable("mercadopago_webhook_events","orcafacil");b.ConfigureBase();b.Property(x=>x.EventKey).HasMaxLength(180);b.HasIndex(x=>x.EventKey).IsUnique();b.HasIndex(x=>x.ExternalPaymentId);}}
