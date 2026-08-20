using System.Text;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence;

public class OrcaFacilDbContext : DbContext
{
    public OrcaFacilDbContext(DbContextOptions<OrcaFacilDbContext> options) : base(options) { }

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<IssuerProfile> IssuerProfiles => Set<IssuerProfile>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentItem> DocumentItems => Set<DocumentItem>();
    public DbSet<PublicQuote> PublicQuotes => Set<PublicQuote>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<SystemError> SystemErrors => Set<SystemError>();
    public DbSet<UserUsage> UserUsage => Set<UserUsage>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AdminSetting> AdminSettings => Set<AdminSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientContact> ClientContacts => Set<ClientContact>();
    public DbSet<ClientTag> ClientTags => Set<ClientTag>();
    public DbSet<ClientTagAssignment> ClientTagAssignments => Set<ClientTagAssignment>();
    public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
    public DbSet<ServiceCatalogItem> ServiceCatalogItems => Set<ServiceCatalogItem>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServicePriceHistory> ServicePriceHistories => Set<ServicePriceHistory>();
    public DbSet<BillingCustomerProfile> BillingCustomerProfiles => Set<BillingCustomerProfile>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<MercadoPagoWebhookEvent> MercadoPagoWebhookEvents => Set<MercadoPagoWebhookEvent>();
    public DbSet<BudgetTemplate> BudgetTemplates => Set<BudgetTemplate>();
    public DbSet<BudgetTemplateItem> BudgetTemplateItems => Set<BudgetTemplateItem>();
    public DbSet<BusinessAccount> BusinessAccounts => Set<BusinessAccount>();
    public DbSet<AccountMember> AccountMembers => Set<AccountMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanVersion> PlanVersions => Set<PlanVersion>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<PlanFeatureValue> PlanFeatureValues => Set<PlanFeatureValue>();
    public DbSet<SubscriptionEvent> SubscriptionEvents => Set<SubscriptionEvent>();
    public DbSet<PlanOverride> PlanOverrides => Set<PlanOverride>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<SupportAccessSession> SupportAccessSessions => Set<SupportAccessSession>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<DocumentRevision> DocumentRevisions => Set<DocumentRevision>();
    public DbSet<PublicDocumentAccess> PublicDocumentAccesses => Set<PublicDocumentAccess>();
    public DbSet<PublicDocumentDecision> PublicDocumentDecisions => Set<PublicDocumentDecision>();
    public DbSet<CommercialFollowUp> CommercialFollowUps => Set<CommercialFollowUp>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderChecklistItem> WorkOrderChecklistItems => Set<WorkOrderChecklistItem>();
    public DbSet<ManualPayment> ManualPayments => Set<ManualPayment>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptSequence> ReceiptSequences => Set<ReceiptSequence>();
    public DbSet<FinancialEntry> FinancialEntries => Set<FinancialEntry>();
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<LegalDocumentVersion> LegalDocumentVersions => Set<LegalDocumentVersion>();
    public DbSet<LegalAcceptance> LegalAcceptances => Set<LegalAcceptance>();
    public DbSet<CommunicationConsent> CommunicationConsents => Set<CommunicationConsent>();
    public DbSet<DataSubjectRequest> DataSubjectRequests => Set<DataSubjectRequest>();
    public DbSet<PrivacyVendor> PrivacyVendors => Set<PrivacyVendor>();
    public DbSet<PrivacyProcessingActivity> PrivacyProcessingActivities => Set<PrivacyProcessingActivity>();
    public DbSet<AccountOnboardingState> AccountOnboardingStates => Set<AccountOnboardingState>();
    public DbSet<CommercialLead> CommercialLeads => Set<CommercialLead>();
    public DbSet<CommercialInteraction> CommercialInteractions => Set<CommercialInteraction>();
    public DbSet<CommercialMessageTemplate> CommercialMessageTemplates => Set<CommercialMessageTemplate>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();
    public DbSet<UserFeedback> UserFeedback => Set<UserFeedback>();
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles => Set<KnowledgeBaseArticle>();
    public DbSet<ReleaseNote> ReleaseNotes => Set<ReleaseNote>();
    public DbSet<AccountSettings> AccountSettings => Set<AccountSettings>();
    public DbSet<DataImport> DataImports => Set<DataImport>();
    public DbSet<RecurringContract> RecurringContracts => Set<RecurringContract>();
    public DbSet<ContractItem> ContractItems => Set<ContractItem>();
    public DbSet<ContractPayment> ContractPayments => Set<ContractPayment>();
    public DbSet<ContractEvent> ContractEvents => Set<ContractEvent>();
    public DbSet<RecommendationCard> RecommendationCards => Set<RecommendationCard>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<AutomationRun> AutomationRuns => Set<AutomationRun>();
    public DbSet<ProductivityEvent> ProductivityEvents => Set<ProductivityEvent>();
    public DbSet<IntegrationSetting> IntegrationSettings => Set<IntegrationSetting>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<DataExport> DataExports => Set<DataExport>();
    public DbSet<FileAsset> FileAssets => Set<FileAsset>();
    public DbSet<FileAssetLink> FileAssetLinks => Set<FileAssetLink>();
    public DbSet<CompanyBrandingProfile> CompanyBrandingProfiles => Set<CompanyBrandingProfile>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<DocumentTemplateVersion> DocumentTemplateVersions => Set<DocumentTemplateVersion>();
    public DbSet<DocumentAuditEvent> DocumentAuditEvents => Set<DocumentAuditEvent>();
    public DbSet<PrivacyConsent> PrivacyConsents => Set<PrivacyConsent>();
    public DbSet<DataExportJob> DataExportJobs => Set<DataExportJob>();
    public DbSet<DataRetentionPolicy> DataRetentionPolicies => Set<DataRetentionPolicy>();
    public DbSet<DataRetentionRun> DataRetentionRuns => Set<DataRetentionRun>();
    public DbSet<SensitiveDataAccessLog> SensitiveDataAccessLogs => Set<SensitiveDataAccessLog>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<SessionRecord> SessionRecords => Set<SessionRecord>();
    public DbSet<PublicTokenAccessLog> PublicTokenAccessLogs => Set<PublicTokenAccessLog>();
    public DbSet<AccountSecuritySetting> AccountSecuritySettings => Set<AccountSecuritySetting>();
    public DbSet<AuditExportJob> AuditExportJobs => Set<AuditExportJob>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<JobLock> JobLocks => Set<JobLock>();
    public DbSet<ProcessingOutboxItem> ProcessingOutbox => Set<ProcessingOutboxItem>();
    public DbSet<SystemMetric> SystemMetrics => Set<SystemMetric>();
    public DbSet<SlowQueryLog> SlowQueryLogs => Set<SlowQueryLog>();
    public DbSet<TenantUsageMetric> TenantUsageMetrics => Set<TenantUsageMetric>();
    public DbSet<CacheInvalidationEvent> CacheInvalidationEvents => Set<CacheInvalidationEvent>();
    public DbSet<QuotaEvent> QuotaEvents => Set<QuotaEvent>();
    public DbSet<RateLimitEvent> RateLimitEvents => Set<RateLimitEvent>();
    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orcafacil");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrcaFacilDbContext).Assembly);
        ApplySnakeCaseColumnNames(modelBuilder);
    }

    private static void ApplySnakeCaseColumnNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0) builder.Append('_');
            builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }
}
