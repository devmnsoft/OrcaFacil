using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public interface IClientShellViewModelFactory
{
    Task<ClientShellViewModel> CreateAsync(CancellationToken cancellationToken = default);
}

public sealed class ClientShellViewModelFactory(
    ICurrentUserService user,
    ICurrentAccountService account,
    INotificationService notifications,
    IPlanExperienceService planExperience,
    OrcaFacilDbContext db) : IClientShellViewModelFactory
{
    public async Task<ClientShellViewModel> CreateAsync(CancellationToken cancellationToken = default)
    {
        var accountName = "Minha conta";
        if (account.AccountId is { } accountId)
            accountName = await db.BusinessAccounts.AsNoTracking()
                .Where(x => x.Id == accountId && !x.IsDeleted)
                .Select(x => x.DisplayName)
                .SingleOrDefaultAsync(cancellationToken) ?? accountName;

        var plan = await planExperience.GetAsync(cancellationToken);
        var permissions = account.AccountRoleCode is null ? [] : await (
            from role in db.Roles.AsNoTracking()
            join rolePermission in db.RolePermissions.AsNoTracking() on role.Id equals rolePermission.RoleId
            join permission in db.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
            where role.Code == account.AccountRoleCode && !role.IsDeleted && !rolePermission.IsDeleted && !permission.IsDeleted
            orderby permission.Code
            select permission.Code).ToArrayAsync(cancellationToken);
        var allowedMenus = ClientMenu.Items.Select(group => new ShellMenuGroup(group.Label,
            group.Items.Where(item => item.RequiredPermission is null || permissions.Contains(item.RequiredPermission)).ToArray()))
            .Where(group => group.Items.Count > 0).ToArray();
        var unread = await notifications.GetUnreadCountAsync(user.UserId, cancellationToken);
        return new ClientShellViewModel(
            user.UserId, FirstName(user.Name), account.AccountId, accountName,
            account.AccountRoleCode ?? "Responsável", plan.SelectedPlanCode, plan.SelectedPlanName,
            plan.EffectivePlanCode, plan.EffectivePlanName, plan.Status, plan.IsUsingFreeFallback,
            unread, allowedMenus, permissions,
            plan.UsageItems.Select(x => new ShellUsageItem(x.Label, x.Used, x.Limit)).ToArray(),
            plan.IsUsingFreeFallback ? plan.ContextualRecommendation : null, null,
            new ShellAction("Novo orçamento", "/Documents/CreateBudget", "budget"),
            "dashboard.overview");
    }

    private static string FirstName(string? name) => string.IsNullOrWhiteSpace(name) ? "bem-vindo" : name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
}

public sealed record ClientShellViewModel(
    Guid UserId, string FirstName, Guid? AccountId, string AccountName, string AccountRole,
    string SelectedPlanCode, string SelectedPlanName, string EffectivePlanCode, string EffectivePlanName,
    string PlanAccessStatus, bool IsUsingFreeFallback, int UnreadNotifications,
    IReadOnlyList<ShellMenuGroup> Menus, IReadOnlyList<string> Permissions,
    IReadOnlyList<ShellUsageItem> UsageSummary, string? PaymentAlert, string? OnboardingSummary,
    ShellAction MainAction, string ContextualHelpCode);
public sealed record ShellMenuGroup(string Label, IReadOnlyList<ShellMenuItem> Items);
public sealed record ShellMenuItem(string Label, string Page, string Icon, bool Premium = false, string? RequiredPlan = null, string? RequiredPermission = null);
public sealed record ShellAction(string Label, string Page, string Icon);
public sealed record ShellUsageItem(string Label, int Used, int? Limit);

internal static class ClientMenu
{
    internal static readonly IReadOnlyList<ShellMenuGroup> Items =
    [
        new("Início", [new("Visão geral", "/Dashboard/Index", "dashboard")]),
        new("Vender", [new("Orçamentos", "/Documents/Index", "budget", RequiredPermission: "documents.read")]),
        new("Receber", [new("Recibos", "/Documents/CreateReceipt", "receipt", RequiredPermission: "receipts.read")]),
        new("Organizar", [new("Clientes", "/Clients/Index", "client", RequiredPermission: "clients.read"), new("Serviços", "/Services/Index", "service", RequiredPermission: "services.read"), new("Modelos", "/Templates/Index", "template", true, "Profissional", "templates.read")]),
        new("Minha conta", [new("Dados do emitente", "/Profile/Index", "account"), new("Meu plano", "/Subscription/Index", "plan"), new("Notificações", "/Notifications/Index", "notification")]),
        new("Aprender", [new("Central de ajuda", "/Support/Index", "help"), new("Conhecer recursos", "/Discover", "demo")])
    ];
}
