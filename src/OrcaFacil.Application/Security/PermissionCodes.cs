namespace OrcaFacil.Application.Security;

/// <summary>Canonical permission identifiers used by backend authorization.</summary>
public static class PermissionCodes
{
    public const string FiscalView="Fiscal.View"; public const string FiscalManage="Fiscal.Manage";
    public const string FiscalConfigureCompany="Fiscal.ConfigureCompany"; public const string FiscalConfigureProvider="Fiscal.ConfigureProvider"; public const string FiscalConfigureCertificate="Fiscal.ConfigureCertificate";
    public const string FiscalServiceCodesView="Fiscal.ServiceCodesView"; public const string FiscalServiceCodesManage="Fiscal.ServiceCodesManage"; public const string FiscalTaxProfilesView="Fiscal.TaxProfilesView"; public const string FiscalTaxProfilesManage="Fiscal.TaxProfilesManage";
    public const string FiscalIssue="Fiscal.Issue"; public const string FiscalManualRegister="Fiscal.ManualRegister"; public const string FiscalCancel="Fiscal.Cancel"; public const string FiscalCorrect="Fiscal.Correct"; public const string FiscalSubstitute="Fiscal.Substitute";
    public const string FiscalDownloadXml="Fiscal.DownloadXml"; public const string FiscalDownloadPdf="Fiscal.DownloadPdf"; public const string FiscalExportAccounting="Fiscal.ExportAccounting"; public const string FiscalReportsView="Fiscal.ReportsView"; public const string FiscalHealthView="Fiscal.HealthView"; public const string PortalFiscalDocumentsView="Portal.FiscalDocumentsView";
    public const string CatalogView = "Catalog.View"; public const string CatalogManage = "Catalog.Manage"; public const string CatalogPublish = "Catalog.Publish";
    public const string PricingView = "Pricing.View"; public const string PricingManage = "Pricing.Manage";
    public const string PricingTablesView = "Pricing.TablesView"; public const string PricingTablesManage = "Pricing.TablesManage";
    public const string PricingCostsView = "Pricing.CostsView"; public const string PricingMarginsView = "Pricing.MarginsView";
    public const string PricingMarginPoliciesManage = "Pricing.MarginPoliciesManage"; public const string PricingDiscountPoliciesManage = "Pricing.DiscountPoliciesManage";
    public const string PricingApprovalsView = "Pricing.ApprovalsView"; public const string PricingApprovalsManage = "Pricing.ApprovalsManage";
    public const string PricingSimulatorUse = "Pricing.SimulatorUse"; public const string PricingRecalculateQuote = "Pricing.RecalculateQuote";
    public const string PricingReportsView = "Pricing.ReportsView"; public const string PricingPackageView = "Pricing.PackageView";
    public const string PricingPackageManage = "Pricing.PackageManage"; public const string PricingRecurringManage = "Pricing.RecurringManage";
    public const string OmnichannelView="Omnichannel.View"; public const string OmnichannelManage="Omnichannel.Manage"; public const string OmnichannelInboxView="Omnichannel.InboxView"; public const string OmnichannelInboxManage="Omnichannel.InboxManage"; public const string OmnichannelConversationsView="Omnichannel.ConversationsView"; public const string OmnichannelConversationsReply="Omnichannel.ConversationsReply"; public const string OmnichannelInternalNotes="Omnichannel.InternalNotes"; public const string OmnichannelAssign="Omnichannel.Assign"; public const string OmnichannelClose="Omnichannel.Close"; public const string OmnichannelChannelsView="Omnichannel.ChannelsView"; public const string OmnichannelChannelsManage="Omnichannel.ChannelsManage"; public const string OmnichannelRoutingView="Omnichannel.RoutingView"; public const string OmnichannelRoutingManage="Omnichannel.RoutingManage"; public const string OmnichannelSlaView="Omnichannel.SlaView"; public const string OmnichannelSlaManage="Omnichannel.SlaManage"; public const string OmnichannelTemplatesView="Omnichannel.TemplatesView"; public const string OmnichannelTemplatesManage="Omnichannel.TemplatesManage"; public const string OmnichannelHealthView="Omnichannel.HealthView"; public const string OmnichannelReportsView="Omnichannel.ReportsView"; public const string OmnichannelCsatView="Omnichannel.CsatView"; public const string OmnichannelCsatManage="Omnichannel.CsatManage";
    public const string AiView="Ai.View"; public const string AiUseCopilot="Ai.UseCopilot"; public const string AiUseRag="Ai.UseRag"; public const string AiUseSemanticSearch="Ai.UseSemanticSearch"; public const string AiAnalyzeDocuments="Ai.AnalyzeDocuments"; public const string AiGenerateDrafts="Ai.GenerateDrafts"; public const string AiApplySuggestions="Ai.ApplySuggestions"; public const string AiManageSettings="Ai.ManageSettings"; public const string AiManageGovernance="Ai.ManageGovernance"; public const string AiManagePromptTemplates="Ai.ManagePromptTemplates"; public const string AiViewLogs="Ai.ViewLogs"; public const string AiViewUsage="Ai.ViewUsage"; public const string AiAdminGlobalView="Ai.AdminGlobalView";
    public const string MarketplaceView="Marketplace.View"; public const string MarketplaceInstall="Marketplace.Install"; public const string MarketplaceRollback="Marketplace.Rollback"; public const string MarketplaceUpdate="Marketplace.Update"; public const string MarketplaceReview="Marketplace.Review"; public const string MarketplaceAdminView="Marketplace.AdminView"; public const string MarketplaceAdminManage="Marketplace.AdminManage";
    public const string TemplatesLibraryView="Templates.LibraryView"; public const string TemplatesLibraryManage="Templates.LibraryManage"; public const string ConfigurationExport="Configuration.Export"; public const string ConfigurationImport="Configuration.Import"; public const string SetupWizardUse="SetupWizard.Use"; public const string AddonsInstall="Addons.Install"; public const string AddonsRemove="Addons.Remove";
    public const string CustomizationView = "Customization.View"; public const string CustomizationManage = "Customization.Manage";
    public const string CustomFieldsView = "CustomFields.View"; public const string CustomFieldsManage = "CustomFields.Manage";
    public const string DynamicFormsView = "DynamicForms.View"; public const string DynamicFormsManage = "DynamicForms.Manage"; public const string DynamicFormsSubmit = "DynamicForms.Submit";
    public const string ChecklistsView = "Checklists.View"; public const string ChecklistsManage = "Checklists.Manage";
    public const string PipelinesView = "Pipelines.View"; public const string PipelinesManage = "Pipelines.Manage";
    public const string WorkflowsView = "Workflows.View"; public const string WorkflowsManage = "Workflows.Manage";
    public const string ProcessTemplatesView = "ProcessTemplates.View"; public const string ProcessTemplatesApply = "ProcessTemplates.Apply";
    public const string ValidationRulesManage = "ValidationRules.Manage"; public const string NotificationRulesManage = "NotificationRules.Manage"; public const string ProcessLogsView = "ProcessLogs.View";
    public const string PartnersView = "Partners.View"; public const string PartnersManage = "Partners.Manage"; public const string PartnersInvite = "Partners.Invite";
    public const string PartnersDocumentsView = "Partners.DocumentsView"; public const string PartnersDocumentsManage = "Partners.DocumentsManage"; public const string PartnersPerformanceView = "Partners.PerformanceView";
    public const string PartnerPortalManage = "PartnerPortal.Manage";
    public const string OutsourcingView = "Outsourcing.View"; public const string OutsourcingManage = "Outsourcing.Manage"; public const string OutsourcingRequest = "Outsourcing.Request";
    public const string OutsourcingQuote = "Outsourcing.Quote"; public const string OutsourcingAssign = "Outsourcing.Assign"; public const string OutsourcingReview = "Outsourcing.Review"; public const string OutsourcingApproveCost = "Outsourcing.ApproveCost";
    public const string PartnerPaymentsView = "PartnerPayments.View"; public const string PartnerPaymentsManage = "PartnerPayments.Manage";
    public const string PartnerMessagesView = "PartnerMessages.View"; public const string PartnerMessagesManage = "PartnerMessages.Manage"; public const string ReportsPartners = "Reports.Partners";
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
    public const string PaymentsConfigureProvider = "Payments.ConfigureProvider";
    public const string PaymentsCheckoutView = "Payments.CheckoutView";
    public const string PaymentsManualConfirm = "Payments.ManualConfirm";
    public const string PaymentsReconcile = "Payments.Reconcile";
    public const string PaymentsRefund = "Payments.Refund";
    public const string PaymentsDisputesView = "Payments.DisputesView";
    public const string PaymentsDisputesManage = "Payments.DisputesManage";
    public const string PaymentsReportsView = "Payments.ReportsView";
    public const string PaymentsWebhooksView = "Payments.WebhooksView";
    public const string PaymentsWebhooksManage = "Payments.WebhooksManage";
    public const string BillingInvoicesView = "Billing.InvoicesView";
    public const string BillingInvoicesManage = "Billing.InvoicesManage";
    public const string BillingDunningView = "Billing.DunningView";
    public const string BillingDunningManage = "Billing.DunningManage";
    public const string BillingSuspensionManage = "Billing.SuspensionManage";
    public const string PortalPaymentsView = "Portal.PaymentsView";
    public const string PartnerPortalCommissionsView = "PartnerPortal.CommissionsView";
    public const string ReceivablesView = "Receivables.View";
    public const string ReceivablesManage = "Receivables.Manage";
    public const string ReceiptsView = "Receipts.View";
    public const string ReceiptsManage = "Receipts.Manage";
    public const string ReceiptsCancel = "Receipts.Cancel";
    public const string FinanceView = "Finance.View";
    public const string FinanceManage = "Finance.Manage";
    public const string FinanceAdvancedView = "Finance.AdvancedView"; public const string FinanceAdvancedManage = "Finance.AdvancedManage";
    public const string FinanceCategoriesView = "Finance.CategoriesView"; public const string FinanceCategoriesManage = "Finance.CategoriesManage";
    public const string FinanceCostCentersView = "Finance.CostCentersView"; public const string FinanceCostCentersManage = "Finance.CostCentersManage";
    public const string FinanceBankAccountsView = "Finance.BankAccountsView"; public const string FinanceBankAccountsManage = "Finance.BankAccountsManage";
    public const string FinanceCashMovementsView = "Finance.CashMovementsView"; public const string FinanceCashMovementsManage = "Finance.CashMovementsManage";
    public const string FinancePayablesView = "Finance.PayablesView"; public const string FinancePayablesManage = "Finance.PayablesManage"; public const string FinancePayablesPay = "Finance.PayablesPay";
    public const string FinanceReconciliationView = "Finance.ReconciliationView"; public const string FinanceReconciliationManage = "Finance.ReconciliationManage";
    public const string FinanceDreView = "Finance.DreView"; public const string FinanceCashflowView = "Finance.CashflowView";
    public const string FinancePeriodClosingManage = "Finance.PeriodClosingManage";
    public const string FinanceManagementView = "FinanceManagement.View"; public const string FinanceManagementManage = "FinanceManagement.Manage";
    public const string FinanceChartAccountsView = "Finance.ChartAccountsView"; public const string FinanceChartAccountsManage = "Finance.ChartAccountsManage";
    public const string FinanceEntriesView = "Finance.EntriesView"; public const string FinanceEntriesManage = "Finance.EntriesManage"; public const string FinanceManualAdjustments = "Finance.ManualAdjustments";
    public const string FinanceCashFlowProjectionView = "Finance.CashFlowProjectionView";
    public const string FinanceBudgetView = "Finance.BudgetView"; public const string FinanceBudgetManage = "Finance.BudgetManage";
    public const string FinanceForecastView = "Finance.ForecastView"; public const string FinanceDreManage = "Finance.DreManage"; public const string FinanceAccrualView = "Finance.AccrualView";
    public const string FinanceAllocationsManage = "Finance.AllocationsManage"; public const string FinanceMonthlyClosingView = "Finance.MonthlyClosingView"; public const string FinanceMonthlyClosingManage = "Finance.MonthlyClosingManage";
    public const string FinanceProfitabilityView = "Finance.ProfitabilityView"; public const string FinanceFinancialAlertsView = "Finance.FinancialAlertsView"; public const string FinanceReportsView = "Finance.ReportsView";
    public const string FinanceExport = "Finance.Export"; public const string FinanceCostsAndMarginsView = "Finance.CostsAndMarginsView";
    public const string FinanceFiscalView = "Finance.FiscalView"; public const string FinanceFiscalManage = "Finance.FiscalManage";
    public const string FinanceImportBankStatement = "Finance.ImportBankStatement"; public const string FinanceExportReports = "Finance.ExportReports";
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
    public const string SupportDeskView = "SupportDesk.View";
    public const string SupportDeskManage = "SupportDesk.Manage";
    public const string SupportQueuesView = "SupportQueues.View";
    public const string SupportQueuesManage = "SupportQueues.Manage";
    public const string SupportAgentsView = "SupportAgents.View";
    public const string SupportAgentsManage = "SupportAgents.Manage";
    public const string SupportTicketsView = "SupportTickets.View";
    public const string SupportTicketsManage = "SupportTickets.Manage";
    public const string SupportTicketsAssign = "SupportTickets.Assign";
    public const string SupportTicketsEscalate = "SupportTickets.Escalate";
    public const string SupportTicketsInternalNotes = "SupportTickets.InternalNotes";
    public const string SupportTicketsReply = "SupportTickets.Reply";
    public const string SupportTicketsClose = "SupportTickets.Close";
    public const string SupportSlaView = "SupportSla.View";
    public const string SupportSlaManage = "SupportSla.Manage";
    public const string SupportIncidentsView = "SupportIncidents.View";
    public const string SupportIncidentsManage = "SupportIncidents.Manage";
    public const string SupportProblemsView = "SupportProblems.View";
    public const string SupportProblemsManage = "SupportProblems.Manage";
    public const string SupportMacrosView = "SupportMacros.View";
    public const string SupportMacrosManage = "SupportMacros.Manage";
    public const string SupportKnowledgeView = "SupportKnowledge.View";
    public const string SupportKnowledgeManage = "SupportKnowledge.Manage";
    public const string SupportCsatView = "SupportCsat.View";
    public const string SupportCsatManage = "SupportCsat.Manage";
    public const string SupportReportsView = "SupportReports.View";
    public const string SupportWorkloadView = "SupportWorkload.View";
    public const string SupportShiftsManage = "SupportShifts.Manage";
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
    public const string ApiKeysView = "ApiKeys.View";
    public const string DeveloperPortalView = "DeveloperPortal.View"; public const string DeveloperPortalManage = "DeveloperPortal.Manage";
    public const string ApiLogsView = "ApiLogs.View"; public const string WebhooksReplay = "Webhooks.Replay";
    public const string ExternalAppsView = "ExternalApps.View"; public const string ExternalAppsManage = "ExternalApps.Manage";
    public const string ConnectorsView = "Connectors.View"; public const string ConnectorsManage = "Connectors.Manage";
    public const string IntegrationHealthView = "IntegrationHealth.View"; public const string AdminApiGlobalView = "Admin.ApiGlobalView";
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
    public const string QualityView = "Quality.View"; public const string QualityManage = "Quality.Manage";
    public const string QualitySourceAuditView = "Quality.SourceAuditView"; public const string QualityBusinessRulesView = "Quality.BusinessRulesView";
    public const string QualityReadinessView = "Quality.ReadinessView"; public const string QualityResolveFindings = "Quality.ResolveFindings";
    public const string QualityExportReports = "Quality.ExportReports";
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
    public const string SuppliersView = "Suppliers.View"; public const string SuppliersManage = "Suppliers.Manage";
    public const string MaterialsView = "Materials.View"; public const string MaterialsManage = "Materials.Manage";
    public const string InventoryView = "Inventory.View"; public const string InventoryManage = "Inventory.Manage";
    public const string InventoryAdjust = "Inventory.Adjust"; public const string InventoryReserve = "Inventory.Reserve"; public const string InventoryConsume = "Inventory.Consume";
    public const string PurchasesView = "Purchases.View"; public const string PurchasesManage = "Purchases.Manage"; public const string PurchasesApprove = "Purchases.Approve"; public const string PurchasesReceive = "Purchases.Receive";
    public const string CostingView = "Costing.View"; public const string CostingManage = "Costing.Manage";
    public const string MarginsView = "Margins.View"; public const string MarginsManagePolicies = "Margins.ManagePolicies";
    public const string PricingRulesView = "PricingRules.View"; public const string PricingRulesManage = "PricingRules.Manage";
    public const string ReportsCosts = "Reports.Costs"; public const string ReportsMargins = "Reports.Margins";
    public const string LocalizationView = "Localization.View"; public const string LocalizationManage = "Localization.Manage";
    public const string LocalizationImport = "Localization.Import"; public const string LocalizationExport = "Localization.Export";
    public const string LocalizationReview = "Localization.Review"; public const string LocaleSettingsView = "LocaleSettings.View";
    public const string LocaleSettingsManage = "LocaleSettings.Manage"; public const string PublicTranslationsView = "PublicTranslations.View";
    public const string PublicTranslationsManage = "PublicTranslations.Manage"; public const string LegalTranslationsManage = "LegalTranslations.Manage";
    public const string SeoTranslationsManage = "SeoTranslations.Manage"; public const string TranslationJobsView = "TranslationJobs.View";
    public const string TranslationJobsManage = "TranslationJobs.Manage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        AiView, AiUseCopilot, AiUseRag, AiUseSemanticSearch, AiAnalyzeDocuments, AiGenerateDrafts, AiApplySuggestions, AiManageSettings, AiManageGovernance, AiManagePromptTemplates, AiViewLogs, AiViewUsage, AiAdminGlobalView,
        MarketplaceView, MarketplaceInstall, MarketplaceRollback, MarketplaceUpdate, MarketplaceReview, MarketplaceAdminView, MarketplaceAdminManage, TemplatesLibraryView, TemplatesLibraryManage, ConfigurationExport, ConfigurationImport, SetupWizardUse, AddonsInstall, AddonsRemove,
        DashboardView, ClientsView, ClientsManage, ServicesView, ServicesManage, DocumentsView,
        DocumentsCreate, DocumentsEdit, DocumentsGeneratePublicLink, DocumentsConvertToWorkOrder,
        CommercialPipelineView, CommercialActionsManage,
        WorkOrdersView, WorkOrdersManage, WorkOrdersCreate, WorkOrdersEdit, WorkOrdersSchedule,
        CustomizationView, CustomizationManage, CustomFieldsView, CustomFieldsManage, DynamicFormsView, DynamicFormsManage,
        DynamicFormsSubmit, ChecklistsView, ChecklistsManage, PipelinesView, PipelinesManage, WorkflowsView, WorkflowsManage,
        ProcessTemplatesView, ProcessTemplatesApply, ValidationRulesManage, NotificationRulesManage, ProcessLogsView,
        WorkOrdersChangeStatus, WorkOrdersCancel, WorkOrdersComplete, WorkOrdersManageChecklist,
        ScheduleView, ScheduleManage, ReportsOperational, PaymentsView, PaymentsManage, PaymentsReverse,
        PaymentsConfigureProvider, PaymentsCheckoutView, PaymentsManualConfirm, PaymentsReconcile, PaymentsRefund,
        PaymentsDisputesView, PaymentsDisputesManage, PaymentsReportsView, PaymentsWebhooksView, PaymentsWebhooksManage,
        BillingInvoicesView, BillingInvoicesManage, BillingDunningView, BillingDunningManage, BillingSuspensionManage,
        PortalPaymentsView, PartnerPortalCommissionsView,
        ReceivablesView, ReceivablesManage, ReceiptsView, ReceiptsManage, ReceiptsCancel,
        FinanceView, FinanceManage, ContractsView, ContractsManage, ContractChargesGenerate, ContractsAdvancedView,
        ContractsAdvancedManage, ContractsSlaView, ContractsSlaManage, ContractsWarrantyView, ContractsWarrantyManage,
        ContractsPreventiveMaintenanceView, ContractsPreventiveMaintenanceManage, ContractsRenewalView, ContractsRenewalManage,
        ContractsAdjustmentsView, ContractsAdjustmentsManage, ContractsAmendmentsView, ContractsAmendmentsManage,
        ContractsUsageView, ContractsHealthView, ContractsReportsView, CashFlowView, ReportsFinancial,
        ReportsView, ReportsExport, SettingsView, SettingsManage, UsersManage, PlanManage, AdminAccess,
        DiagnosticsView, LogsView, AuditView, SupportView, SupportManage, SupportCreateTicket, SupportManageTickets, SupportDeskView, SupportDeskManage, SupportQueuesView, SupportQueuesManage, SupportAgentsView, SupportAgentsManage, SupportTicketsView, SupportTicketsManage, SupportTicketsAssign, SupportTicketsEscalate, SupportTicketsInternalNotes, SupportTicketsReply, SupportTicketsClose, SupportSlaView, SupportSlaManage, SupportIncidentsView, SupportIncidentsManage, SupportProblemsView, SupportProblemsManage, SupportMacrosView, SupportMacrosManage, SupportKnowledgeView, SupportKnowledgeManage, SupportCsatView, SupportCsatManage, SupportReportsView, SupportWorkloadView, SupportShiftsManage,
        FeedbackView, FeedbackCreate, KnowledgeBaseManage, ReleaseNotesManage, SetupChecklistView, SetupChecklistManage,
        RecommendationsView, AutomationRulesView, AutomationRulesManage, ProductivityView, ExecutiveReportsView,
        ScoresView, ScoresFinancialDetails, IntegrationsView, IntegrationsManage, WebhooksView, WebhooksManage,
        ApiKeysManage, ApiKeysView, DeveloperPortalView, DeveloperPortalManage, ApiLogsView, WebhooksReplay,
        ExternalAppsView, ExternalAppsManage, ConnectorsView, ConnectorsManage, IntegrationHealthView, AdminApiGlobalView,
        ImportsManage, ExportsManage, NotificationsManage, CommunicationPreferencesManage,
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
        DataQualityManage, QualityView, QualityManage, QualitySourceAuditView, QualityBusinessRulesView,
        QualityReadinessView, QualityResolveFindings, QualityExportReports, AccountHealthView, ExecutiveAlertsView, DashboardCustomize,
        CrmView, CrmManage, CrmSegmentsView, CrmSegmentsManage, CrmInteractionsView, CrmInteractionsManage,
        CrmCampaignsView, CrmCampaignsManage, CrmSurveysView, CrmSurveysManage, NpsView, NpsManage,
        CustomerSuccessView, CustomerSuccessManage, RetentionView, RetentionManage, UpsellView, UpsellManage,
        CommunicationOptOutManage, SuppliersView, SuppliersManage, MaterialsView, MaterialsManage,
        InventoryView, InventoryManage, InventoryAdjust, InventoryReserve, InventoryConsume,
        PurchasesView, PurchasesManage, PurchasesApprove, PurchasesReceive, CostingView, CostingManage,
        MarginsView, MarginsManagePolicies, PricingRulesView, PricingRulesManage, ReportsCosts, ReportsMargins,
        LocalizationView, LocalizationManage, LocalizationImport, LocalizationExport, LocalizationReview,
        LocaleSettingsView, LocaleSettingsManage, PublicTranslationsView, PublicTranslationsManage,
        LegalTranslationsManage, SeoTranslationsManage, TranslationJobsView, TranslationJobsManage
    };
}
