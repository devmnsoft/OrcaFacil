namespace OrcaFacil.Application.Security;

/// <summary>Canonical permission identifiers used by backend authorization.</summary>
public static class PermissionCodes
{
    public const string DashboardView = "Dashboard.View";
    public const string ClientsView = "Clients.View";
    public const string ClientsManage = "Clients.Manage";
    public const string ServicesView = "Services.View";
    public const string ServicesManage = "Services.Manage";
    public const string DocumentsView = "Documents.View";
    public const string DocumentsCreate = "Documents.Create";
    public const string DocumentsEdit = "Documents.Edit";
    public const string DocumentsGeneratePublicLink = "Documents.GeneratePublicLink";
    public const string DocumentsConvertToWorkOrder = "Documents.ConvertToWorkOrder";
    public const string CommercialPipelineView = "CommercialPipeline.View";
    public const string CommercialActionsManage = "CommercialActions.Manage";
    public const string WorkOrdersView = "WorkOrders.View";
    public const string WorkOrdersManage = "WorkOrders.Manage";
    public const string WorkOrdersCreate = "WorkOrders.Create";
    public const string WorkOrdersEdit = "WorkOrders.Edit";
    public const string WorkOrdersSchedule = "WorkOrders.Schedule";
    public const string WorkOrdersChangeStatus = "WorkOrders.ChangeStatus";
    public const string WorkOrdersCancel = "WorkOrders.Cancel";
    public const string WorkOrdersComplete = "WorkOrders.Complete";
    public const string WorkOrdersManageChecklist = "WorkOrders.ManageChecklist";
    public const string ScheduleView = "Schedule.View";
    public const string ScheduleManage = "Schedule.Manage";
    public const string ReportsOperational = "Reports.Operational";
    public const string PaymentsView = "Payments.View";
    public const string PaymentsManage = "Payments.Manage";
    public const string PaymentsReverse = "Payments.Reverse";
    public const string ReceivablesView = "Receivables.View";
    public const string ReceivablesManage = "Receivables.Manage";
    public const string ReceiptsView = "Receipts.View";
    public const string ReceiptsManage = "Receipts.Manage";
    public const string ReceiptsCancel = "Receipts.Cancel";
    public const string FinanceView = "Finance.View";
    public const string FinanceManage = "Finance.Manage";
    public const string ContractsView = "Contracts.View";
    public const string ContractsManage = "Contracts.Manage";
    public const string ContractChargesGenerate = "ContractCharges.Generate";
    public const string ContractsAdvancedView = "Contracts.AdvancedView"; public const string ContractsAdvancedManage = "Contracts.AdvancedManage";
    public const string ContractsSlaView = "Contracts.SlaView"; public const string ContractsSlaManage = "Contracts.SlaManage";
    public const string ContractsWarrantyView = "Contracts.WarrantyView"; public const string ContractsWarrantyManage = "Contracts.WarrantyManage";
    public const string ContractsPreventiveMaintenanceView = "Contracts.PreventiveMaintenanceView"; public const string ContractsPreventiveMaintenanceManage = "Contracts.PreventiveMaintenanceManage";
    public const string ContractsRenewalView = "Contracts.RenewalView"; public const string ContractsRenewalManage = "Contracts.RenewalManage";
    public const string ContractsAdjustmentsView = "Contracts.AdjustmentsView"; public const string ContractsAdjustmentsManage = "Contracts.AdjustmentsManage";
    public const string ContractsAmendmentsView = "Contracts.AmendmentsView"; public const string ContractsAmendmentsManage = "Contracts.AmendmentsManage";
    public const string ContractsUsageView = "Contracts.UsageView"; public const string ContractsHealthView = "Contracts.HealthView"; public const string ContractsReportsView = "Contracts.ReportsView";
    public const string CashFlowView = "CashFlow.View";
    public const string ReportsFinancial = "Reports.Financial";
    public const string ReportsView = "Reports.View";
    public const string ReportsExport = "Reports.Export";
    public const string SupportManage = "Support.Manage";
    public const string SettingsView = "Settings.View";
    public const string SettingsManage = "Settings.Manage";
    public const string UsersManage = "Users.Manage";
    public const string PlanManage = "Plan.Manage";
    public const string AdminAccess = "Admin.Access";
    public const string DiagnosticsView = "Diagnostics.View";
    public const string LogsView = "Logs.View";
    public const string AuditView = "Audit.View";
    public const string SupportView = "Support.View";
    public const string SupportCreateTicket = "Support.CreateTicket";
    public const string SupportManageTickets = "Support.ManageTickets";
    public const string FeedbackView = "Feedback.View";
    public const string FeedbackCreate = "Feedback.Create";
    public const string KnowledgeBaseManage = "KnowledgeBase.Manage";
    public const string ReleaseNotesManage = "ReleaseNotes.Manage";
    public const string SetupChecklistView = "SetupChecklist.View";
    public const string SetupChecklistManage = "SetupChecklist.Manage";
    public const string RecommendationsView = "Recommendations.View";
    public const string AutomationRulesView = "AutomationRules.View";
    public const string AutomationRulesManage = "AutomationRules.Manage";
    public const string ProductivityView = "Productivity.View";
    public const string ExecutiveReportsView = "ExecutiveReports.View";
    public const string ScoresView = "Scores.View";
    public const string ScoresFinancialDetails = "Scores.FinancialDetails";
    public const string IntegrationsView = "Integrations.View";
    public const string IntegrationsManage = "Integrations.Manage";
    public const string WebhooksView = "Webhooks.View";
    public const string WebhooksManage = "Webhooks.Manage";
    public const string ApiKeysManage = "ApiKeys.Manage";
    public const string ImportsManage = "Imports.Manage";
    public const string ExportsManage = "Exports.Manage";
    public const string NotificationsManage = "Notifications.Manage";
    public const string CommunicationPreferencesManage = "CommunicationPreferences.Manage";
    public const string FilesView = "Files.View";
    public const string FilesUpload = "Files.Upload";
    public const string FilesDownload = "Files.Download";
    public const string FilesDelete = "Files.Delete";
    public const string DocumentTemplatesView = "DocumentTemplates.View";
    public const string DocumentTemplatesManage = "DocumentTemplates.Manage";
    public const string DocumentsPrint = "Documents.Print";
    public const string DocumentsExportPdf = "Documents.ExportPdf";
    public const string ReceiptsPrint = "Receipts.Print";
    public const string ReceiptsExportPdf = "Receipts.ExportPdf";
    public const string WorkOrdersPrint = "WorkOrders.Print";
    public const string ContractsPrint = "Contracts.Print";
    public const string BrandingManage = "Branding.Manage";
    public const string PrivacyView = "Privacy.View";
    public const string PrivacyManage = "Privacy.Manage";
    public const string PrivacyExportData = "Privacy.ExportData";
    public const string PrivacyAnonymizeData = "Privacy.AnonymizeData";
    public const string PrivacyManageRetention = "Privacy.ManageRetention";
    public const string AuditExport = "Audit.Export";
    public const string SensitiveDataView = "SensitiveData.View";
    public const string SecurityViewSessions = "Security.ViewSessions";
    public const string SecurityManageSessions = "Security.ManageSessions";
    public const string SecurityManageUsers = "Security.ManageUsers";
    public const string TokensRevoke = "Tokens.Revoke";
    public const string FilesDownloadPrivate = "Files.DownloadPrivate";
    public const string FilesManageVisibility = "Files.ManageVisibility";
    public const string BusinessUnitsView = "BusinessUnits.View"; public const string BusinessUnitsManage = "BusinessUnits.Manage";
    public const string TeamsView = "Teams.View"; public const string TeamsManage = "Teams.Manage";
    public const string RolesView = "Roles.View"; public const string RolesManage = "Roles.Manage";
    public const string ApprovalsView = "Approvals.View"; public const string ApprovalsRequest = "Approvals.Request"; public const string ApprovalsDecide = "Approvals.Decide";
    public const string DiscountPoliciesView = "DiscountPolicies.View"; public const string DiscountPoliciesManage = "DiscountPolicies.Manage";
    public const string VisibilityRulesManage = "VisibilityRules.Manage";
    public const string WhiteLabelView = "WhiteLabel.View"; public const string WhiteLabelManage = "WhiteLabel.Manage";
    public const string ReportsByUnit = "Reports.ByUnit"; public const string ReportsByTeam = "Reports.ByTeam";
    public const string BillingViewOwn = "Billing.ViewOwn"; public const string BillingManageOwn = "Billing.ManageOwn";
    public const string BillingAdminView = "Billing.AdminView"; public const string BillingAdminManage = "Billing.AdminManage";
    public const string PlansView = "Plans.View"; public const string PlansManage = "Plans.Manage";
    public const string SubscriptionsView = "Subscriptions.View"; public const string SubscriptionsManage = "Subscriptions.Manage";
    public const string SubscriptionRequestsCreate = "SubscriptionRequests.Create"; public const string SubscriptionRequestsManage = "SubscriptionRequests.Manage";
    public const string InvoicesViewOwn = "Invoices.ViewOwn"; public const string InvoicesManage = "Invoices.Manage";
    public const string BillingPaymentsManage = "BillingPayments.Manage"; public const string AddonsView = "Addons.View"; public const string AddonsManage = "Addons.Manage";
    public const string EntitlementsView = "Entitlements.View"; public const string EntitlementsManage = "Entitlements.Manage";
    public const string AccountAccessManage = "AccountAccess.Manage"; public const string TrialsManage = "Trials.Manage";
    public const string SearchGlobal = "Search.Global"; public const string CommandCenterUse = "CommandCenter.Use";
    public const string AssistantUse = "Assistant.Use"; public const string KnowledgeBaseView = "KnowledgeBase.View";
    public const string GuidedToursView = "GuidedTours.View"; public const string GuidedToursManage = "GuidedTours.Manage";
    public const string OnboardingManage = "Onboarding.Manage"; public const string ActivityView = "Activity.View";
    public const string ShortcutsManageOwn = "Shortcuts.ManageOwn"; public const string FavoritesManageOwn = "Favorites.ManageOwn";
    public const string AnalyticsView = "Analytics.View"; public const string AnalyticsExecutive = "Analytics.Executive";
    public const string AnalyticsFinancial = "Analytics.Financial"; public const string AnalyticsOperational = "Analytics.Operational";
    public const string AnalyticsForecast = "Analytics.Forecast"; public const string AnalyticsExport = "Analytics.Export";
    public const string GoalsView = "Goals.View"; public const string GoalsManage = "Goals.Manage";
    public const string DataQualityView = "DataQuality.View"; public const string DataQualityManage = "DataQuality.Manage";
    public const string AccountHealthView = "AccountHealth.View"; public const string ExecutiveAlertsView = "ExecutiveAlerts.View";
    public const string DashboardCustomize = "Dashboard.Customize";
    public const string CrmView = "Crm.View"; public const string CrmManage = "Crm.Manage";
    public const string CrmSegmentsView = "CrmSegments.View"; public const string CrmSegmentsManage = "CrmSegments.Manage";
    public const string CrmInteractionsView = "CrmInteractions.View"; public const string CrmInteractionsManage = "CrmInteractions.Manage";
    public const string CrmCampaignsView = "CrmCampaigns.View"; public const string CrmCampaignsManage = "CrmCampaigns.Manage";
    public const string CrmSurveysView = "CrmSurveys.View"; public const string CrmSurveysManage = "CrmSurveys.Manage";
    public const string NpsView = "Nps.View"; public const string NpsManage = "Nps.Manage";
    public const string CustomerSuccessView = "CustomerSuccess.View"; public const string CustomerSuccessManage = "CustomerSuccess.Manage";
    public const string RetentionView = "Retention.View"; public const string RetentionManage = "Retention.Manage";
    public const string UpsellView = "Upsell.View"; public const string UpsellManage = "Upsell.Manage";
    public const string CommunicationOptOutManage = "CommunicationOptOut.Manage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DashboardView, ClientsView, ClientsManage, ServicesView, ServicesManage, DocumentsView,
        DocumentsCreate, DocumentsEdit, DocumentsGeneratePublicLink, DocumentsConvertToWorkOrder,
        CommercialPipelineView, CommercialActionsManage,
        WorkOrdersView, WorkOrdersManage, WorkOrdersCreate, WorkOrdersEdit, WorkOrdersSchedule,
        WorkOrdersChangeStatus, WorkOrdersCancel, WorkOrdersComplete, WorkOrdersManageChecklist,
        ScheduleView, ScheduleManage, ReportsOperational, PaymentsView, PaymentsManage, PaymentsReverse,
        ReceivablesView, ReceivablesManage, ReceiptsView, ReceiptsManage, ReceiptsCancel,
        FinanceView, FinanceManage, ContractsView, ContractsManage, ContractChargesGenerate, ContractsAdvancedView,
        ContractsAdvancedManage, ContractsSlaView, ContractsSlaManage, ContractsWarrantyView, ContractsWarrantyManage,
        ContractsPreventiveMaintenanceView, ContractsPreventiveMaintenanceManage, ContractsRenewalView, ContractsRenewalManage,
        ContractsAdjustmentsView, ContractsAdjustmentsManage, ContractsAmendmentsView, ContractsAmendmentsManage,
        ContractsUsageView, ContractsHealthView, ContractsReportsView, CashFlowView, ReportsFinancial,
        ReportsView, ReportsExport, SettingsView, SettingsManage, UsersManage, PlanManage, AdminAccess,
        DiagnosticsView, LogsView, AuditView, SupportView, SupportManage, SupportCreateTicket, SupportManageTickets,
        FeedbackView, FeedbackCreate, KnowledgeBaseManage, ReleaseNotesManage, SetupChecklistView, SetupChecklistManage,
        RecommendationsView, AutomationRulesView, AutomationRulesManage, ProductivityView, ExecutiveReportsView,
        ScoresView, ScoresFinancialDetails, IntegrationsView, IntegrationsManage, WebhooksView, WebhooksManage,
        ApiKeysManage, ImportsManage, ExportsManage, NotificationsManage, CommunicationPreferencesManage,
        FilesView, FilesUpload, FilesDownload, FilesDelete, DocumentTemplatesView, DocumentTemplatesManage,
        DocumentsPrint, DocumentsExportPdf, ReceiptsPrint, ReceiptsExportPdf, WorkOrdersPrint, ContractsPrint, BrandingManage,
        PrivacyView, PrivacyManage, PrivacyExportData, PrivacyAnonymizeData, PrivacyManageRetention, AuditExport,
        SensitiveDataView, SecurityViewSessions, SecurityManageSessions, SecurityManageUsers, TokensRevoke,
        FilesDownloadPrivate, FilesManageVisibility, BusinessUnitsView, BusinessUnitsManage, TeamsView, TeamsManage,
        RolesView, RolesManage, ApprovalsView, ApprovalsRequest, ApprovalsDecide, DiscountPoliciesView,
        DiscountPoliciesManage, VisibilityRulesManage, WhiteLabelView, WhiteLabelManage, ReportsByUnit, ReportsByTeam,
        BillingViewOwn, BillingManageOwn, BillingAdminView, BillingAdminManage, PlansView, PlansManage,
        SubscriptionsView, SubscriptionsManage, SubscriptionRequestsCreate, SubscriptionRequestsManage,
        InvoicesViewOwn, InvoicesManage, BillingPaymentsManage, AddonsView, AddonsManage, EntitlementsView,
        EntitlementsManage, AccountAccessManage, TrialsManage, SearchGlobal, CommandCenterUse, AssistantUse,
        KnowledgeBaseView, GuidedToursView, GuidedToursManage, OnboardingManage, ActivityView,
        ShortcutsManageOwn, FavoritesManageOwn, AnalyticsView, AnalyticsExecutive, AnalyticsFinancial,
        AnalyticsOperational, AnalyticsForecast, AnalyticsExport, GoalsView, GoalsManage, DataQualityView,
        DataQualityManage, AccountHealthView, ExecutiveAlertsView, DashboardCustomize,
        CrmView, CrmManage, CrmSegmentsView, CrmSegmentsManage, CrmInteractionsView, CrmInteractionsManage,
        CrmCampaignsView, CrmCampaignsManage, CrmSurveysView, CrmSurveysManage, NpsView, NpsManage,
        CustomerSuccessView, CustomerSuccessManage, RetentionView, RetentionManage, UpsellView, UpsellManage,
        CommunicationOptOutManage
    };
}
