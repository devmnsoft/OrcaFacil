using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders; using OrcaFacil.Domain.Entities;
namespace OrcaFacil.Persistence.Configurations;
public class PaymentEventConfiguration:IEntityTypeConfiguration<PaymentEvent>{public void Configure(EntityTypeBuilder<PaymentEvent>b){b.ToTable("payment_events","orcafacil");b.ConfigureBase();b.Property(x=>x.Action).HasMaxLength(120);b.Property(x=>x.Status).HasMaxLength(40);b.HasIndex(x=>x.PaymentId);}}
