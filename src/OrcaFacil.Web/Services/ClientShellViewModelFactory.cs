using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Saas.Modules;
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
    SaasModuleRegistryService modules,
    IModuleEntitlementService entitlements,
    OrcaFacilDbContext db) : IClientShellViewModelFactory
{
    public async Task<ClientShellViewModel> CreateAsync(CancellationToken cancellationToken = default)
    {
        var platformOnly = account.IsPlatformUser && !account.HasAccount;
        var accountName = platformOnly ? "Administração da plataforma" : "Minha conta";
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
        var allowedMenus = platformOnly
            ? new[]
            {
                new ShellMenuGroup("Plataforma",
                    [new ShellMenuItem("Administração", "/SuperAdmin/Index", "settings")],
                    true)
            }
            : await FilterByContractAsync(navigation.GetGroups(permissions), cancellationToken);
        var unread = await notifications.GetUnreadCountAsync(user.UserId, cancellationToken);
        return new ClientShellViewModel(
            user.UserId, FirstName(user.Name), MaskEmail(user.Email), account.AccountId, accountName,
            platformOnly ? "SuperAdmin" : account.AccountRoleCode ?? "Responsável", plan.SelectedPlanCode, plan.SelectedPlanName,
            plan.EffectivePlanCode, plan.EffectivePlanName, plan.Status, plan.IsUsingFreeFallback,
            unread, allowedMenus, permissions,
            plan.UsageItems.Select(x => new ShellUsageItem(x.Label, x.Used, x.Limit)).ToArray(),
            plan.IsUsingFreeFallback ? plan.ContextualRecommendation : null, null,
            new ShellAction("Novo orçamento", "/Documents/CreateBudget", "quote"),
            "dashboard.overview");
    }

    private async Task<ShellMenuGroup[]> FilterByContractAsync(IReadOnlyList<NavigationGroup> groups, CancellationToken cancellationToken)
    {
        var visible = new List<ShellMenuGroup>();
        foreach (var group in groups)
        {
            var items = new List<ShellMenuItem>();
            foreach (var item in group.Items)
            {
                var module = modules.FindByPath(item.Page);
                if (module is not null)
                {
                    var decision = await entitlements.CheckAsync(account.AccountId, module.Code, false, cancellationToken);
                    if (!decision.Allowed) continue;
                }
                items.Add(new ShellMenuItem(item.Label, item.Page, item.Icon, RequiredPermission: item.RequiredPermission));
            }
            if (items.Count > 0) visible.Add(new ShellMenuGroup(group.Label, items, group.Primary));
        }
        return visible.ToArray();
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
public sealed record ShellMenuGroup(string Label, IReadOnlyList<ShellMenuItem> Items, bool Primary = true);
public sealed record ShellMenuItem(string Label, string Page, string Icon, bool Premium = false, string? RequiredPlan = null, string? RequiredPermission = null);
public sealed record ShellAction(string Label, string Page, string Icon);
public sealed record ShellUsageItem(string Label, int Used, int? Limit);
