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
    public DbSet<BillingInvoiceItem> BillingInvoiceItems => Set<BillingInvoiceItem>();
    public DbSet<BillingPayment> BillingPayments => Set<BillingPayment>();
    public DbSet<SubscriptionChangeRequest> SubscriptionChangeRequests => Set<SubscriptionChangeRequest>();
    public DbSet<PlanAddon> PlanAddons => Set<PlanAddon>();
    public DbSet<AccountAddon> AccountAddons => Set<AccountAddon>();
    public DbSet<AccountEntitlement> AccountEntitlements => Set<AccountEntitlement>();
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
    public DbSet<ContractSlaPolicy> ContractSlaPolicies => Set<ContractSlaPolicy>();
    public DbSet<ContractSlaEvent> ContractSlaEvents => Set<ContractSlaEvent>();
    public DbSet<ServiceLevelBreach> ServiceLevelBreaches => Set<ServiceLevelBreach>();
    public DbSet<ContractWarrantyTerm> ContractWarrantyTerms => Set<ContractWarrantyTerm>();
    public DbSet<ContractPreventiveSchedule> ContractPreventiveSchedules => Set<ContractPreventiveSchedule>();
    public DbSet<ContractRecurrenceRun> ContractRecurrenceRuns => Set<ContractRecurrenceRun>();
    public DbSet<ContractUsageAllowance> ContractUsageAllowances => Set<ContractUsageAllowance>();
    public DbSet<ContractAmendment> ContractAmendments => Set<ContractAmendment>();
    public DbSet<ContractAdjustment> ContractAdjustments => Set<ContractAdjustment>();
    public DbSet<ContractRenewalEvent> ContractRenewalEvents => Set<ContractRenewalEvent>();
    public DbSet<ContractHealthSnapshot> ContractHealthSnapshots => Set<ContractHealthSnapshot>();
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
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<BusinessUnitMember> BusinessUnitMembers => Set<BusinessUnitMember>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<RoleProfile> RoleProfiles => Set<RoleProfile>();
    public DbSet<RoleProfilePermission> RoleProfilePermissions => Set<RoleProfilePermission>();
    public DbSet<DiscountPolicy> DiscountPolicies => Set<DiscountPolicy>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalRequestEvent> ApprovalRequestEvents => Set<ApprovalRequestEvent>();
    public DbSet<WhiteLabelSetting> WhiteLabelSettings => Set<WhiteLabelSetting>();
    public DbSet<UnitBrandingProfile> UnitBrandingProfiles => Set<UnitBrandingProfile>();
    public DbSet<DocumentVisibilityRule> DocumentVisibilityRules => Set<DocumentVisibilityRule>();
    public DbSet<BusinessGoal> BusinessGoals => Set<BusinessGoal>();
    public DbSet<GoalProgressSnapshot> GoalProgressSnapshots => Set<GoalProgressSnapshot>();
    public DbSet<AnalyticsSnapshot> AnalyticsSnapshots => Set<AnalyticsSnapshot>();
    public DbSet<AnalyticsSnapshotItem> AnalyticsSnapshotItems => Set<AnalyticsSnapshotItem>();
    public DbSet<ForecastSnapshot> ForecastSnapshots => Set<ForecastSnapshot>();
    public DbSet<DataQualityFinding> DataQualityFindings => Set<DataQualityFinding>();
    public DbSet<DashboardWidgetPreference> DashboardWidgetPreferences => Set<DashboardWidgetPreference>();
    public DbSet<ClientRelationshipProfile> ClientRelationshipProfiles => Set<ClientRelationshipProfile>();
    public DbSet<ClientInteraction> ClientInteractions => Set<ClientInteraction>();
    public DbSet<ClientHealthScore> ClientHealthScores => Set<ClientHealthScore>();
    public DbSet<CommunicationOptOut> CommunicationOptOuts => Set<CommunicationOptOut>();
    public DbSet<NpsResponse> NpsResponses => Set<NpsResponse>();
    public DbSet<RetentionRiskEvent> RetentionRiskEvents => Set<RetentionRiskEvent>();
    public DbSet<CrmOpportunity> CrmOpportunities => Set<CrmOpportunity>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<MaterialUnit> MaterialUnits => Set<MaterialUnit>();
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialSupplierPrice> MaterialSupplierPrices => Set<MaterialSupplierPrice>();
    public DbSet<InventoryLocation> InventoryLocations => Set<InventoryLocation>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryStockMovement> InventoryStockMovements => Set<InventoryStockMovement>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<CostComposition> CostCompositions => Set<CostComposition>();
    public DbSet<CostCompositionItem> CostCompositionItems => Set<CostCompositionItem>();
    public DbSet<DocumentCostSnapshot> DocumentCostSnapshots => Set<DocumentCostSnapshot>();
    public DbSet<DocumentMarginSnapshot> DocumentMarginSnapshots => Set<DocumentMarginSnapshot>();
    public DbSet<MarginPolicy> MarginPolicies => Set<MarginPolicy>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<Payable> Payables => Set<Payable>();
    public DbSet<PayablePayment> PayablePayments => Set<PayablePayment>();
    public DbSet<PayableRecurrence> PayableRecurrences => Set<PayableRecurrence>();
    public DbSet<FinancialPeriodClosing> FinancialPeriodClosings => Set<FinancialPeriodClosing>();
    public DbSet<FiscalDocumentRequest> FiscalDocumentRequests => Set<FiscalDocumentRequest>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankReconciliationSession> BankReconciliationSessions => Set<BankReconciliationSession>();
    public DbSet<BankReconciliationMatch> BankReconciliationMatches => Set<BankReconciliationMatch>();
    public DbSet<FinancialImportBatch> FinancialImportBatches => Set<FinancialImportBatch>();
    public DbSet<FinancialImportRow> FinancialImportRows => Set<FinancialImportRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orcafacil");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrcaFacilDbContext).Assembly);
        modelBuilder.Entity<MaterialUnit>().ToTable("material_units");
        modelBuilder.Entity<MaterialCategory>().ToTable("material_categories");
        modelBuilder.Entity<MaterialSupplierPrice>().ToTable("material_supplier_prices");
        modelBuilder.Entity<InventoryLocation>().ToTable("inventory_locations");
        modelBuilder.Entity<InventoryReservation>().ToTable("inventory_reservations");
        modelBuilder.Entity<PurchaseRequest>().ToTable("purchase_requests");
        modelBuilder.Entity<PurchaseRequestItem>().ToTable("purchase_request_items");
        modelBuilder.Entity<PurchaseOrder>().ToTable("purchase_orders");
        modelBuilder.Entity<PurchaseOrderItem>().ToTable("purchase_order_items");
        modelBuilder.Entity<CostComposition>().ToTable("cost_compositions");
        modelBuilder.Entity<CostCompositionItem>().ToTable("cost_composition_items");
        modelBuilder.Entity<DocumentCostSnapshot>().ToTable("document_cost_snapshots");
        modelBuilder.Entity<DocumentMarginSnapshot>().ToTable("document_margin_snapshots");
        modelBuilder.Entity<FinancialCategory>().ToTable("financial_categories").HasIndex(x => new { x.AccountId, x.Code }).IsUnique();
        modelBuilder.Entity<CostCenter>().ToTable("cost_centers").HasIndex(x => new { x.AccountId, x.Code }).IsUnique();
        modelBuilder.Entity<BankAccount>().ToTable("bank_accounts");
        modelBuilder.Entity<CashMovement>().ToTable("cash_movements").HasIndex(x => new { x.AccountId, x.IdempotencyKey }).IsUnique();
        modelBuilder.Entity<Payable>().ToTable("payables");
        modelBuilder.Entity<PayablePayment>().ToTable("payable_payments").HasIndex(x => new { x.AccountId, x.IdempotencyKey }).IsUnique();
        modelBuilder.Entity<PayableRecurrence>().ToTable("payable_recurrences");
        modelBuilder.Entity<FinancialPeriodClosing>().ToTable("financial_period_closings").HasIndex(x => new { x.AccountId, x.PeriodStart, x.PeriodEnd }).IsUnique();
        modelBuilder.Entity<FiscalDocumentRequest>().ToTable("fiscal_document_requests");
        modelBuilder.Entity<BankTransaction>().ToTable("bank_transactions").HasIndex(x => new { x.AccountId, x.Fingerprint }).IsUnique();
        modelBuilder.Entity<BankReconciliationSession>().ToTable("bank_reconciliation_sessions");
        modelBuilder.Entity<BankReconciliationMatch>().ToTable("bank_reconciliation_matches").HasIndex(x => new { x.AccountId, x.BankTransactionId }).IsUnique().HasFilter("reversed_at IS NULL AND is_deleted=false");
        modelBuilder.Entity<FinancialImportBatch>().ToTable("financial_import_batches").HasIndex(x => new { x.AccountId, x.BankAccountId, x.FileHash }).IsUnique();
        modelBuilder.Entity<FinancialImportRow>().ToTable("financial_import_rows").HasIndex(x => new { x.AccountId, x.BatchId, x.RowNumber }).IsUnique();
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
