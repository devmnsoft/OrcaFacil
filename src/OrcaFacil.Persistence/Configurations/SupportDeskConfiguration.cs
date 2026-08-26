using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Configurations;

public sealed class SupportDeskConfiguration :
    IEntityTypeConfiguration<SupportQueue>, IEntityTypeConfiguration<SupportQueueMember>,
    IEntityTypeConfiguration<SupportSlaPolicy>, IEntityTypeConfiguration<SupportTicketEvent>,
    IEntityTypeConfiguration<SupportTicketSlaEvent>, IEntityTypeConfiguration<SupportTicketEscalation>,
    IEntityTypeConfiguration<SupportCsatSurvey>, IEntityTypeConfiguration<SupportCsatResponse>,
    IEntityTypeConfiguration<SupportIncident>, IEntityTypeConfiguration<SupportIncidentImpactedAccount>,
    IEntityTypeConfiguration<SupportProblemRecord>, IEntityTypeConfiguration<SupportKnowledgeArticle>,
    IEntityTypeConfiguration<SupportMacro>, IEntityTypeConfiguration<SupportShiftSchedule>
{
    private static void Base<TEntity>(EntityTypeBuilder<TEntity> b, string table) where TEntity : OrcaFacil.Domain.Common.Entity
    { b.ToTable(table, "orcafacil"); b.ConfigureBase(); b.HasQueryFilter(x => !x.IsDeleted); }

    public void Configure(EntityTypeBuilder<SupportQueue> b) { Base(b,"support_queues"); b.Property(x=>x.Name).HasMaxLength(120).IsRequired(); b.Property(x=>x.Level).HasMaxLength(16); b.HasIndex(x=>new{x.AccountId,x.Name}).IsUnique(); }
    public void Configure(EntityTypeBuilder<SupportQueueMember> b) { Base(b,"support_queue_members"); b.Property(x=>x.Level).HasMaxLength(16); b.HasIndex(x=>new{x.QueueId,x.UserId}).IsUnique(); }
    public void Configure(EntityTypeBuilder<SupportSlaPolicy> b) { Base(b,"support_ticket_sla_policies"); b.Property(x=>x.Name).HasMaxLength(120).IsRequired(); b.HasIndex(x=>new{x.AccountId,x.PriorityId,x.QueueId}); }
    public void Configure(EntityTypeBuilder<SupportTicketEvent> b) { Base(b,"support_ticket_events"); b.Property(x=>x.Type).HasMaxLength(50).IsRequired(); b.Property(x=>x.Details).HasMaxLength(2000); b.HasIndex(x=>new{x.AccountId,x.TicketId,x.CreatedAt}); }
    public void Configure(EntityTypeBuilder<SupportTicketSlaEvent> b) { Base(b,"support_ticket_sla_events"); b.Property(x=>x.Type).HasMaxLength(50).IsRequired(); b.HasIndex(x=>new{x.TicketId,x.Type}).IsUnique(); }
    public void Configure(EntityTypeBuilder<SupportTicketEscalation> b) { Base(b,"support_ticket_escalations"); b.Property(x=>x.Reason).HasMaxLength(1000).IsRequired(); b.HasIndex(x=>new{x.AccountId,x.TicketId,x.CreatedAt}); }
    public void Configure(EntityTypeBuilder<SupportCsatSurvey> b) { Base(b,"support_ticket_csat_surveys"); b.Property(x=>x.TokenHash).HasMaxLength(64).IsRequired(); b.HasIndex(x=>x.TicketId).IsUnique(); b.HasIndex(x=>x.TokenHash).IsUnique(); }
    public void Configure(EntityTypeBuilder<SupportCsatResponse> b) { Base(b,"support_ticket_csat_responses"); b.Property(x=>x.Comment).HasMaxLength(2000); b.HasIndex(x=>x.SurveyId).IsUnique(); }
    public void Configure(EntityTypeBuilder<SupportIncident> b) { Base(b,"support_incidents"); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x=>x.Severity).HasMaxLength(8); }
    public void Configure(EntityTypeBuilder<SupportIncidentImpactedAccount> b) { Base(b,"support_incident_impacted_accounts"); b.HasIndex(x=>new{x.IncidentId,x.AccountId}).IsUnique(); }
    public void Configure(EntityTypeBuilder<SupportProblemRecord> b) { Base(b,"support_problem_records"); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(24); }
    public void Configure(EntityTypeBuilder<SupportKnowledgeArticle> b) { Base(b,"support_knowledge_articles"); b.Property(x=>x.Title).HasMaxLength(180).IsRequired(); b.Property(x=>x.Visibility).HasMaxLength(20); b.HasIndex(x=>new{x.AccountId,x.IsPublished}); }
    public void Configure(EntityTypeBuilder<SupportMacro> b) { Base(b,"support_macros"); b.Property(x=>x.Name).HasMaxLength(120).IsRequired(); b.Property(x=>x.Visibility).HasMaxLength(20); b.HasIndex(x=>new{x.AccountId,x.Name}).IsUnique(); }
    public void Configure(EntityTypeBuilder<SupportShiftSchedule> b) { Base(b,"support_shift_schedules"); b.HasIndex(x=>new{x.QueueId,x.StartAt,x.EndAt}); }
}
