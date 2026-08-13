using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class RecurringContractConfiguration : IEntityTypeConfiguration<RecurringContract>
{
    public void Configure(EntityTypeBuilder<RecurringContract> b)
    {
        b.ToTable("recurring_contracts"); b.ConfigureBase();
        b.Property(x => x.Number).HasMaxLength(40).IsRequired(); b.Property(x => x.Title).HasMaxLength(180).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Periodicity).HasConversion<string>().HasMaxLength(24); b.Property(x => x.RecurringAmount).HasPrecision(18, 2);
        b.Property(x => x.CommercialTerms).HasMaxLength(4000); b.Property(x => x.InternalNotes).HasMaxLength(4000); b.Property(x => x.CustomerNotes).HasMaxLength(4000);
        b.Property(x => x.Priority).HasMaxLength(20); b.Property(x => x.ServiceHours).HasMaxLength(120); b.Property(x => x.SlaNotes).HasMaxLength(1000); b.Property(x => x.CancellationReason).HasMaxLength(500);
        b.HasIndex(x => new { x.AccountId, x.Number }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.SourceDocumentId }).HasFilter("source_document_id IS NOT NULL").IsUnique();
        b.HasIndex(x => new { x.AccountId, x.ClientId, x.Status }); b.HasIndex(x => new { x.AccountId, x.EndDate });
        b.HasIndex(x => new { x.AccountId, x.NextBillingDate }); b.HasIndex(x => new { x.AccountId, x.NextServiceDate });
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContractItemConfiguration : IEntityTypeConfiguration<ContractItem>
{
    public void Configure(EntityTypeBuilder<ContractItem> b) { b.ToTable("contract_items"); b.ConfigureBase(); b.Property(x => x.Description).HasMaxLength(500).IsRequired(); b.Property(x => x.Quantity).HasPrecision(18, 4); b.Property(x => x.UnitPrice).HasPrecision(18, 2); b.Property(x => x.Checklist).HasColumnType("text"); b.HasIndex(x => new { x.AccountId, x.ContractId }); }
}
public sealed class ContractPaymentConfiguration : IEntityTypeConfiguration<ContractPayment>
{
    public void Configure(EntityTypeBuilder<ContractPayment> b) { b.ToTable("contract_payments"); b.ConfigureBase(); b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.PaymentMethod).HasMaxLength(40); b.Property(x => x.Notes).HasMaxLength(1000); b.HasIndex(x => new { x.AccountId, x.ContractId, x.Competence }).IsUnique(); b.HasIndex(x => new { x.AccountId, x.Status, x.DueDate }); }
}
public sealed class ContractEventConfiguration : IEntityTypeConfiguration<ContractEvent>
{
    public void Configure(EntityTypeBuilder<ContractEvent> b) { b.ToTable("contract_events"); b.ConfigureBase(); b.Property(x => x.Type).HasMaxLength(50); b.Property(x => x.Description).HasMaxLength(1000); b.Property(x => x.RelatedEntityType).HasMaxLength(50); b.Property(x => x.RelatedUrl).HasMaxLength(500); b.HasIndex(x => new { x.AccountId, x.ContractId, x.CreatedAt }); }
}
