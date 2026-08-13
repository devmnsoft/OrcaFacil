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
    INavigationMapService navigation,
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
        var allowedMenus = navigation.GetGroups(permissions).Select(group => new ShellMenuGroup(group.Label,
            group.Items.Select(item => new ShellMenuItem(item.Label, item.Page, item.Icon, RequiredPermission: item.RequiredPermission)).ToArray())).ToArray();
        var unread = await notifications.GetUnreadCountAsync(user.UserId, cancellationToken);
        return new ClientShellViewModel(
            user.UserId, FirstName(user.Name), MaskEmail(user.Email), account.AccountId, accountName,
            account.AccountRoleCode ?? "Responsável", plan.SelectedPlanCode, plan.SelectedPlanName,
            plan.EffectivePlanCode, plan.EffectivePlanName, plan.Status, plan.IsUsingFreeFallback,
            unread, allowedMenus, permissions,
            plan.UsageItems.Select(x => new ShellUsageItem(x.Label, x.Used, x.Limit)).ToArray(),
            plan.IsUsingFreeFallback ? plan.ContextualRecommendation : null, null,
            new ShellAction("Novo orçamento", "/Documents/CreateBudget", "quote"),
            "dashboard.overview");
    }

    private static string FirstName(string? name) => string.IsNullOrWhiteSpace(name) ? "bem-vindo" : name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "E-mail não informado";
        var parts = email.Split('@', 2);
        var visible = parts[0].Length > 1 ? parts[0][..2] : parts[0][..1];
        return $"{visible}***@{parts[1]}";
    }
}

public sealed record ClientShellViewModel(
    Guid UserId, string FirstName, string MaskedEmail, Guid? AccountId, string AccountName, string AccountRole,
    string SelectedPlanCode, string SelectedPlanName, string EffectivePlanCode, string EffectivePlanName,
    string PlanAccessStatus, bool IsUsingFreeFallback, int UnreadNotifications,
    IReadOnlyList<ShellMenuGroup> Menus, IReadOnlyList<string> Permissions,
    IReadOnlyList<ShellUsageItem> UsageSummary, string? PaymentAlert, string? OnboardingSummary,
    ShellAction MainAction, string ContextualHelpCode);
public sealed record ShellMenuGroup(string Label, IReadOnlyList<ShellMenuItem> Items);
public sealed record ShellMenuItem(string Label, string Page, string Icon, bool Premium = false, string? RequiredPlan = null, string? RequiredPermission = null);
public sealed record ShellAction(string Label, string Page, string Icon);
public sealed record ShellUsageItem(string Label, int Used, int? Limit);
