using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Saas.Modules;

namespace OrcaFacil.Web.Services;

public sealed record ModuleMenuItem(string Group, string Label, string Page, string ModuleCode);

public interface IUserMenuComposer { Task<IReadOnlyList<ModuleMenuItem>> ComposeAsync(CancellationToken ct = default); }
public interface IAccountMenuComposer : IUserMenuComposer { }
public interface ISuperAdminMenuComposer : IUserMenuComposer { }

public sealed class AccountMenuComposer(ICurrentAccountService current, IModuleEntitlementService entitlements,
    SaasModuleRegistryService registry) : IAccountMenuComposer
{
    private static readonly ModuleMenuItem[] Items =
    [
        new("Principal", "Dashboard", "/Dashboard/Index", "CORE"),
        new("Comercial", "Clientes", "/Clients/Index", "CLIENTS"),
        new("Comercial", "Orçamentos e propostas", "/Documents/Index", "DOCUMENTS"),
        new("Comercial", "Rotina comercial", "/CommercialRoutine/Index", "COMMERCIAL_ROUTINE"),
        new("Operações", "Ordens de serviço", "/WorkOrders/Index", "WORK_ORDERS"),
        new("Operações", "Agenda", "/Schedule/Index", "SCHEDULE"),
        new("Gestão", "Financeiro", "/CashFlow/Index", "FINANCIAL"),
        new("Relacionamento", "Suporte", "/Support/Index", "SUPPORT"),
        new("Sucesso", "Treinamento", "/Training/Index", "TRAINING"),
        new("Administração", "Administração da conta", "/AccountAdmin/Index", "ACCOUNT_ADMIN")
    ];

    public async Task<IReadOnlyList<ModuleMenuItem>> ComposeAsync(CancellationToken ct = default)
    {
        if (current.AccountId is not Guid accountId) return [];
        var result = new List<ModuleMenuItem>();
        foreach (var item in Items)
        {
            if (item.ModuleCode == "ACCOUNT_ADMIN" && current.AccountRoleCode is not ("Owner" or "Administrator")) continue;
            var definition = registry.Find(item.ModuleCode);
            if (definition is null || !definition.IsActive) continue;
            var hasPermission = item.ModuleCode == "ACCOUNT_ADMIN" && current.AccountRoleCode is "Owner" or "Administrator";
            if (!hasPermission) hasPermission = await current.HasPermissionAsync(definition.RequiredPermissionCode, ct);
            if (!hasPermission) continue;
            var decision = await entitlements.CheckAsync(accountId, item.ModuleCode, false, ct);
            if (decision.Allowed) result.Add(item);
        }
        return result;
    }
}

public sealed class SuperAdminMenuComposer(IHttpContextAccessor accessor) : ISuperAdminMenuComposer
{
    private static readonly ModuleMenuItem[] Items =
    [
        new("Global", "Visão geral", "/SuperAdmin/Index", "GLOBAL"), new("Global", "Clientes", "/SuperAdmin/Clients/Index", "GLOBAL"),
        new("Global", "Módulos e preços", "/SuperAdmin/Modules/Index", "GLOBAL"), new("Global", "Usuários", "/SuperAdmin/Users/Index", "GLOBAL"),
        new("Operação", "Billing", "/SuperAdmin/Billing/Index", "GLOBAL"), new("Operação", "Uso por cliente", "/SuperAdmin/Usage/Index", "GLOBAL"),
        new("Governança", "Auditoria", "/SuperAdmin/Audit/Index", "GLOBAL"), new("Governança", "System Health", "/SystemHealth/Database", "GLOBAL"),
        new("Governança", "Quality Gate", "/Admin/QualityGate", "GLOBAL")
    ];
    public Task<IReadOnlyList<ModuleMenuItem>> ComposeAsync(CancellationToken ct = default)
    {
        var user = accessor.HttpContext?.User;
        if (user?.IsInRole("SuperAdministrator") == true || user?.IsInRole("SuperAdmin") == true)
            return Task.FromResult<IReadOnlyList<ModuleMenuItem>>(Items);
        var allowed = Items.Where(item =>
            item.Page is "/SuperAdmin/Usage/Index" or "/SuperAdmin/Audit/Index" or "/SystemHealth/Database" ||
            item.Page == "/SuperAdmin/Billing/Index" && (user?.IsInRole("GlobalBilling") == true || user?.IsInRole("PlatformFinance") == true));
        return Task.FromResult<IReadOnlyList<ModuleMenuItem>>(allowed.ToArray());
    }
}

public sealed class UserMenuComposer(IHttpContextAccessor accessor, IAccountMenuComposer account, ISuperAdminMenuComposer global) : IUserMenuComposer
{
    public Task<IReadOnlyList<ModuleMenuItem>> ComposeAsync(CancellationToken ct = default)
    {
        var user = accessor.HttpContext?.User;
        var isGlobal = user?.Claims.Any(claim => claim.Type == System.Security.Claims.ClaimTypes.Role && claim.Value is
            "SuperAdministrator" or "SuperAdmin" or "PlatformSupport" or "PlatformFinance" or "PlatformAuditor" or
            "GlobalSupport" or "GlobalBilling" or "GlobalAuditor") == true;
        return isGlobal ? global.ComposeAsync(ct) : account.ComposeAsync(ct);
    }
}
