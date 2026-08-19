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
    public const string WorkOrdersView = "WorkOrders.View";
    public const string WorkOrdersManage = "WorkOrders.Manage";
    public const string PaymentsView = "Payments.View";
    public const string PaymentsManage = "Payments.Manage";
    public const string ReceiptsView = "Receipts.View";
    public const string ReceiptsManage = "Receipts.Manage";
    public const string FinanceView = "Finance.View";
    public const string FinanceManage = "Finance.Manage";
    public const string ContractsView = "Contracts.View";
    public const string ContractsManage = "Contracts.Manage";
    public const string CashFlowView = "CashFlow.View";
    public const string ReportsFinancial = "Reports.Financial";
    public const string ReportsView = "Reports.View";
    public const string ReportsExport = "Reports.Export";
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

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DashboardView, ClientsView, ClientsManage, ServicesView, ServicesManage, DocumentsView,
        DocumentsCreate, DocumentsEdit, DocumentsGeneratePublicLink, DocumentsConvertToWorkOrder,
        WorkOrdersView, WorkOrdersManage, PaymentsView, PaymentsManage, ReceiptsView, ReceiptsManage,
        FinanceView, FinanceManage, ContractsView, ContractsManage, CashFlowView, ReportsFinancial,
        ReportsView, ReportsExport, SettingsView, SettingsManage, UsersManage, PlanManage, AdminAccess,
        DiagnosticsView, LogsView, AuditView, SupportView, SupportCreateTicket, SupportManageTickets,
        FeedbackView, FeedbackCreate, KnowledgeBaseManage, ReleaseNotesManage, SetupChecklistView, SetupChecklistManage
    };
}
