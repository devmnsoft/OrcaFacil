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

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DashboardView, ClientsView, ClientsManage, ServicesView, ServicesManage, DocumentsView,
        DocumentsCreate, DocumentsEdit, DocumentsGeneratePublicLink, DocumentsConvertToWorkOrder,
        WorkOrdersView, WorkOrdersManage, PaymentsView, PaymentsManage, ReceiptsView, ReceiptsManage,
        ReportsView, ReportsExport, SettingsView, SettingsManage, UsersManage, PlanManage, AdminAccess,
        DiagnosticsView, LogsView, AuditView
    };
}
