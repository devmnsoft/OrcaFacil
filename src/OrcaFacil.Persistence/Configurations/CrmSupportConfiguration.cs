using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class CommercialInteractionConfiguration : IEntityTypeConfiguration<CommercialInteraction>
{
    public void Configure(EntityTypeBuilder<CommercialInteraction> b)
    {
        b.ToTable("commercial_interactions", "orcafacil"); b.ConfigureBase();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(24); b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Summary).HasMaxLength(1200).IsRequired(); b.HasIndex(x => new { x.AccountId, x.NextFollowUpAt });
        b.HasOne<BusinessAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CommercialLead>().WithMany().HasForeignKey(x => x.LeadId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CommercialMessageTemplateConfiguration : IEntityTypeConfiguration<CommercialMessageTemplate>
{
    public void Configure(EntityTypeBuilder<CommercialMessageTemplate> b)
    {
        b.ToTable("commercial_message_templates", "orcafacil");
        b.ConfigureBase();
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Name).HasMaxLength(140).IsRequired();
        b.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(180);
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.AccountId, x.Code }).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> b)
    {
        b.ToTable("support_tickets", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Protocol).HasMaxLength(24).IsRequired();
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(24); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.Subject).HasMaxLength(180).IsRequired(); b.Property(x => x.Description).HasMaxLength(5000).IsRequired(); b.Property(x => x.InternalNotes).HasMaxLength(4000);
        b.Property(x => x.RelatedPage).HasMaxLength(300); b.Property(x => x.CorrelationId).HasMaxLength(100); b.Property(x => x.BrowserInfo).HasMaxLength(500);
        b.HasIndex(x => x.Protocol).IsUnique(); b.HasIndex(x => new { x.AccountId, x.Status, x.CreatedAt });
        b.HasOne<BusinessAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict); b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.OpenedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserFeedbackConfiguration : IEntityTypeConfiguration<UserFeedback>
{
    public void Configure(EntityTypeBuilder<UserFeedback> b) { b.ToTable("user_feedback", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.PageUrl).HasMaxLength(500).IsRequired(); b.Property(x=>x.Rating).HasMaxLength(32).IsRequired(); b.Property(x=>x.Message).HasMaxLength(2000); b.Property(x=>x.BrowserInfo).HasMaxLength(500); b.Property(x=>x.CorrelationId).HasMaxLength(100); b.HasIndex(x=>new{x.AccountId,x.CreatedAt}); }
}
public sealed class KnowledgeBaseArticleConfiguration : IEntityTypeConfiguration<KnowledgeBaseArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeBaseArticle> b) { b.ToTable("knowledge_base_articles", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.Slug).HasMaxLength(180).IsRequired(); b.Property(x=>x.Summary).HasMaxLength(500).IsRequired(); b.Property(x=>x.Content).HasMaxLength(12000).IsRequired(); b.Property(x=>x.Category).HasMaxLength(80).IsRequired(); b.Property(x=>x.Audience).HasMaxLength(24).IsRequired(); b.HasIndex(x=>x.Slug).IsUnique(); }
}
public sealed class ReleaseNoteConfiguration : IEntityTypeConfiguration<ReleaseNote>
{
    public void Configure(EntityTypeBuilder<ReleaseNote> b) { b.ToTable("release_notes", "orcafacil"); b.ConfigureBase(); b.Property(x=>x.Version).HasMaxLength(30).IsRequired(); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.Description).HasMaxLength(5000).IsRequired(); b.Property(x=>x.Category).HasMaxLength(32).IsRequired(); b.HasIndex(x=>new{x.IsPublished,x.ReleasedAt}); }
}

public sealed class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> b)
    {
        b.ToTable("support_ticket_messages", "orcafacil"); b.ConfigureBase(); b.Property(x => x.Body).HasMaxLength(5000).IsRequired(); b.HasIndex(x => new { x.TicketId, x.CreatedAt });
        b.HasOne<SupportTicket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade); b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
