using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients", "orcafacil");
        builder.ConfigureBase();
        builder.Property(x => x.PersonType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.DocumentNumber).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.TradeName).HasMaxLength(180);
        builder.Property(x => x.LegalName).HasMaxLength(180);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.City).HasMaxLength(120);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.InternalNotes).HasMaxLength(2000);
        builder.Property(x => x.PreferredContactChannel).HasMaxLength(24);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.DocumentNumber);
        builder.HasIndex(x => new { x.AccountId, x.IsActive }).HasDatabaseName("ix_clients_account_active");
        builder.HasIndex(x => new { x.AccountId, x.Name }).HasDatabaseName("ix_clients_account_name");
    }
}

public sealed class ClientContactConfiguration : IEntityTypeConfiguration<ClientContact>
{
    public void Configure(EntityTypeBuilder<ClientContact> builder)
    {
        builder.ToTable("client_contacts", "orcafacil"); builder.ConfigureBase();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ContactType).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Value).HasMaxLength(254).IsRequired(); builder.Property(x => x.Label).HasMaxLength(60);
        builder.HasIndex(x => new { x.AccountId, x.ClientId, x.SortOrder });
    }
}
public sealed class ClientTagConfiguration : IEntityTypeConfiguration<ClientTag>
{
    public void Configure(EntityTypeBuilder<ClientTag> builder) { builder.ToTable("client_tags", "orcafacil"); builder.ConfigureBase(); builder.Property(x => x.Name).HasMaxLength(60).IsRequired(); builder.Property(x => x.NormalizedName).HasMaxLength(60).IsRequired(); builder.Property(x => x.ColorToken).HasMaxLength(32); builder.HasIndex(x => new { x.AccountId, x.NormalizedName }).IsUnique(); }
}
public sealed class ClientTagAssignmentConfiguration : IEntityTypeConfiguration<ClientTagAssignment>
{
    public void Configure(EntityTypeBuilder<ClientTagAssignment> builder) { builder.ToTable("client_tag_assignments", "orcafacil"); builder.HasKey(x => new { x.AccountId, x.ClientId, x.ClientTagId }); }
}
public sealed class ClientNoteConfiguration : IEntityTypeConfiguration<ClientNote>
{
    public void Configure(EntityTypeBuilder<ClientNote> builder) { builder.ToTable("client_notes", "orcafacil"); builder.ConfigureBase(); builder.Property(x => x.Content).HasMaxLength(4000).IsRequired(); builder.HasIndex(x => new { x.AccountId, x.ClientId, x.CreatedAt }); }
}
