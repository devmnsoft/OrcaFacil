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

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DashboardView, ClientsView, ClientsManage, ServicesView, ServicesManage, DocumentsView,
        DocumentsCreate, DocumentsEdit, DocumentsGeneratePublicLink, DocumentsConvertToWorkOrder,
        CommercialPipelineView, CommercialActionsManage,
        WorkOrdersView, WorkOrdersManage, WorkOrdersCreate, WorkOrdersEdit, WorkOrdersSchedule,
        WorkOrdersChangeStatus, WorkOrdersCancel, WorkOrdersComplete, WorkOrdersManageChecklist,
        ScheduleView, ScheduleManage, ReportsOperational, PaymentsView, PaymentsManage, PaymentsReverse,
        ReceivablesView, ReceivablesManage, ReceiptsView, ReceiptsManage, ReceiptsCancel,
        FinanceView, FinanceManage, ContractsView, ContractsManage, ContractChargesGenerate, CashFlowView, ReportsFinancial,
        ReportsView, ReportsExport, SettingsView, SettingsManage, UsersManage, PlanManage, AdminAccess,
        DiagnosticsView, LogsView, AuditView, SupportView, SupportManage, SupportCreateTicket, SupportManageTickets,
        FeedbackView, FeedbackCreate, KnowledgeBaseManage, ReleaseNotesManage, SetupChecklistView, SetupChecklistManage,
        RecommendationsView, AutomationRulesView, AutomationRulesManage, ProductivityView, ExecutiveReportsView,
        ScoresView, ScoresFinancialDetails
    };
}
